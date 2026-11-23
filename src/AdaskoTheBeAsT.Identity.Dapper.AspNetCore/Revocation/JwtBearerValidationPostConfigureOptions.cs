using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Revocation;

public sealed class JwtBearerValidationPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private const string ChallengeDetailItemKey = "AdaskoTheBeAsT.Identity.Dapper.AspNetCore.ChallengeDetail";

    private readonly IOptions<IdentityJwtValidationOptions> _validationOptions;
    private readonly IJwtValidationKeysProvider _keysProvider;
    private readonly TimeProvider _timeProvider;

    public JwtBearerValidationPostConfigureOptions(
        IOptions<IdentityJwtValidationOptions> validationOptions,
        IJwtValidationKeysProvider keysProvider,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(validationOptions);
        ArgumentNullException.ThrowIfNull(keysProvider);
        _validationOptions = validationOptions;
        _keysProvider = keysProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void PostConfigure(
        string? name,
        JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var cfg = _validationOptions.Value;
        if (!string.Equals(name, cfg.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.TimeProvider ??= _timeProvider;
        options.MapInboundClaims = cfg.MapInboundClaims;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg.Issuer,
            ValidateAudience = true,
            ValidAudiences = cfg.Audiences.ToArray(),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = _keysProvider.GetValidationKeys(),
            ValidAlgorithms = _keysProvider.GetValidAlgorithms(),
            ClockSkew = cfg.ClockSkew,
            LifetimeValidator = ValidateLifetimeWithTimeProvider,
        };

        options.Events ??= new JwtBearerEvents();
        ChainTokenValidated(options.Events, cfg);
        ChainChallenge(options.Events, options.Challenge);
        ChainForbidden(options.Events);
    }

    private static void ChainTokenValidated(
        JwtBearerEvents events,
        IdentityJwtValidationOptions cfg)
    {
        var existingHandler = events.OnTokenValidated;
        events.OnTokenValidated = async context =>
        {
            if (existingHandler != null)
            {
                await existingHandler(context).ConfigureAwait(false);
                if (context.Result != null)
                {
                    return;
                }
            }

            await EnforceRevocationCheckAsync(context, cfg).ConfigureAwait(false);
        };
    }

    private static void ChainChallenge(
        JwtBearerEvents events,
        string challengeScheme)
    {
        var existingChallenge = events.OnChallenge;
        events.OnChallenge = async context =>
        {
            if (existingChallenge != null)
            {
                await existingChallenge(context).ConfigureAwait(false);
                if (context.Handled || context.Response.HasStarted)
                {
                    return;
                }
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            AppendWwwAuthenticateHeader(context, challengeScheme);

            if (HttpMethods.IsHead(context.Request.Method))
            {
                return;
            }

            var (errorCode, detail) = ResolveChallengeDetails(context);
            var problem = Results.Problem(
                detail: detail,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [IdentityJwtProblemDetailsDefaults.ErrorExtensionKey] = errorCode,
                });

            await problem.ExecuteAsync(context.HttpContext).ConfigureAwait(false);
        };
    }

    private static void ChainForbidden(JwtBearerEvents events)
    {
        var existingForbidden = events.OnForbidden;
        events.OnForbidden = async context =>
        {
            if (existingForbidden != null)
            {
                await existingForbidden(context).ConfigureAwait(false);
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            if (HttpMethods.IsHead(context.Request.Method))
            {
                return;
            }

            var problem = Results.Problem(
                detail: "You do not have permission to access this resource.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [IdentityJwtProblemDetailsDefaults.ErrorExtensionKey] = "forbidden",
                });

            await problem.ExecuteAsync(context.HttpContext).ConfigureAwait(false);
        };
    }

    private static void AppendWwwAuthenticateHeader(
        JwtBearerChallengeContext context,
        string challengeScheme)
    {
        var scheme = string.IsNullOrWhiteSpace(challengeScheme)
            ? JwtBearerDefaults.AuthenticationScheme
            : challengeScheme;

        if (string.IsNullOrWhiteSpace(context.Error) && context.AuthenticateFailure == null)
        {
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, scheme);
            return;
        }

        var errorToken = IsSafeHeaderToken(context.Error) ? context.Error! : "invalid_token";
        context.Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{scheme} error=\"{errorToken}\"");
    }

    private static bool IsSafeHeaderToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            var valid = (ch >= 'a' && ch <= 'z') ||
                        (ch >= 'A' && ch <= 'Z') ||
                        (ch >= '0' && ch <= '9') ||
                        ch == '_' ||
                        ch == '-';
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    private static (string ErrorCode, string Detail) ResolveChallengeDetails(JwtBearerChallengeContext context)
    {
        if (context.HttpContext.Items.TryGetValue(ChallengeDetailItemKey, out var itemObj) &&
            itemObj is string explicitDetail)
        {
            return ("invalid_token", explicitDetail);
        }

        if (context.AuthenticateFailure is SecurityTokenExpiredException)
        {
            return ("invalid_token", "The access token has expired.");
        }

        if (context.AuthenticateFailure != null || !string.IsNullOrWhiteSpace(context.Error))
        {
            var code = !string.IsNullOrWhiteSpace(context.Error) ? context.Error : "invalid_token";
            return (code, "The access token is invalid.");
        }

        return ("unauthorized", "Authentication is required to access this resource.");
    }

    private static async Task EnforceRevocationCheckAsync(
        TokenValidatedContext context,
        IdentityJwtValidationOptions cfg)
    {
        var store = context.HttpContext.RequestServices.GetService<ITokenRevocationStore>();
        if (store == null)
        {
            return;
        }

        var checkContext = ExtractCheckContext(context.SecurityToken, cfg.Issuer);
        try
        {
            var revoked = await store.IsRevokedAsync(checkContext, context.HttpContext.RequestAborted).ConfigureAwait(false);
            if (revoked)
            {
                context.HttpContext.Items[ChallengeDetailItemKey] = "The access token or session has been revoked.";
                context.Fail("The access token or session has been revoked.");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILoggerFactory>()?
                .CreateLogger<JwtBearerValidationPostConfigureOptions>();
            if (logger != null)
            {
                LogRevocationStoreFailure(logger, cfg.FailureMode, ex);
            }

            if (cfg.FailureMode == RevocationFailureMode.FailClosed)
            {
                context.HttpContext.Items[ChallengeDetailItemKey] = "Token revocation verification failed.";
                context.Fail("Token revocation verification failed.");
            }
        }
    }

    private static TokenRevocationCheckContext ExtractCheckContext(
        SecurityToken securityToken,
        string fallbackIssuer)
    {
        if (securityToken is JsonWebToken jsonWebToken)
        {
            var issuer = string.IsNullOrWhiteSpace(jsonWebToken.Issuer) ? fallbackIssuer : jsonWebToken.Issuer;
            var jti = jsonWebToken.Id;
            var sub = jsonWebToken.Subject;
            var sid = jsonWebToken.TryGetPayloadValue<string>(IdentityJwtClaimTypes.SessionId, out var sidVal) ? sidVal : null;
            DateTimeOffset? issuedAt = jsonWebToken.IssuedAt != DateTime.MinValue
                ? new DateTimeOffset(DateTime.SpecifyKind(jsonWebToken.IssuedAt, DateTimeKind.Utc))
                : null;
            return new TokenRevocationCheckContext(issuer, jti, sid, sub, issuedAt);
        }

        if (securityToken is JwtSecurityToken legacyJwt)
        {
            var issuer = string.IsNullOrWhiteSpace(legacyJwt.Issuer) ? fallbackIssuer : legacyJwt.Issuer;
            var jti = legacyJwt.Id;
            var sub = legacyJwt.Subject;
            var sid = legacyJwt.Claims.FirstOrDefault(
                c => string.Equals(c.Type, IdentityJwtClaimTypes.SessionId, StringComparison.Ordinal))?.Value;
            DateTimeOffset? issuedAt = legacyJwt.IssuedAt != DateTime.MinValue
                ? new DateTimeOffset(DateTime.SpecifyKind(legacyJwt.IssuedAt, DateTimeKind.Utc))
                : null;
            return new TokenRevocationCheckContext(issuer, jti, sid, sub, issuedAt);
        }

        return new TokenRevocationCheckContext(fallbackIssuer, null, null, null, null);
    }

    private static void LogRevocationStoreFailure(
        ILogger logger,
        RevocationFailureMode failureMode,
        Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                exception,
                "Token revocation store check failed under mode {FailureMode}.",
                failureMode);
        }
    }

    private bool ValidateLifetimeWithTimeProvider(
        DateTime? notBefore,
        DateTime? expires,
        SecurityToken? securityToken,
        TokenValidationParameters validationParameters)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var skew = validationParameters.ClockSkew;
        if (notBefore.HasValue && now + skew < DateTime.SpecifyKind(notBefore.Value, DateTimeKind.Utc))
        {
            return false;
        }

        return expires.HasValue && now - skew < DateTime.SpecifyKind(expires.Value, DateTimeKind.Utc);
    }
}
