using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Models;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;

public static class IdentityJwtEndpointRouteBuilderExtensions
{
    private static readonly AuthorizeAttribute BearerAuthorizeAttribute = new()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    };

    public static IdentityJwtEndpointsConventionBuilder MapIdentityJwtApi<TUser>(
        this IEndpointRouteBuilder endpoints,
        Action<IdentityJwtEndpointOptions>? configure = null)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var endpointOptions = new IdentityJwtEndpointOptions();
        configure?.Invoke(endpointOptions);

        var builders = new List<IEndpointConventionBuilder>();
        MapLoginAndRefreshEndpoints<TUser>(endpoints, builders);
        MapRevokeEndpoint<TUser>(endpoints, builders);

        if (endpointOptions.EnableMfaEndpoints)
        {
            MapMfaLoginAndStatusEndpoints<TUser>(endpoints, builders);
            MapMfaManagementEndpoints<TUser>(endpoints, builders);

            if (endpointOptions.EnableMfaSetupLinkEndpoints)
            {
                MapMfaSetupLinkEndpoints<TUser>(endpoints, builders);
            }
        }

        if (endpointOptions.EnableRegistration)
        {
            MapRegistrationEndpoint<TUser>(endpoints, builders);
        }

        return new IdentityJwtEndpointsConventionBuilder(builders);
    }

    private static void MapLoginAndRefreshEndpoints<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/login",
                        (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtLoginRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtLoginResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType));

        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/refresh",
                        (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtRefreshRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await authService.RefreshAsync(request, cancellationToken).ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtLoginResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType));
    }

    private static void MapRevokeEndpoint<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/revoke",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                        {
                            var (bodyOk, request) = await TryReadOptionalJsonAsync<JwtRevokeRequest>(httpContext, cancellationToken)
                                .ConfigureAwait(false);
                            if (!bodyOk)
                            {
                                return CreateMalformedJsonProblem();
                            }

                            var result = await authService.RevokeAsync(httpContext.User, request, cancellationToken).ConfigureAwait(false);
                            return result.Succeeded ? Results.NoContent() : ToErrorResult(result);
                        })
                    .Produces(StatusCodes.Status204NoContent)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized));
    }

    private static void MapMfaLoginAndStatusEndpoints<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/login/mfa",
                        (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtMfaVerifyRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await authService.VerifyMfaAsync(request, cancellationToken).ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtLoginResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType));

        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/login/mfa/resend",
                        (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtMfaResendRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await authService.ResendMfaCodeAsync(request, cancellationToken).ConfigureAwait(false);
                                    if (result is { Status: JwtOperationStatus.TooManyRequests, Value.RetryAfterSeconds: > 0 } cooldown)
                                    {
                                        httpContext.Response.Headers.RetryAfter =
                                            cooldown.Value.RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
                                    }

                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtMfaResendResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType,
                StatusCodes.Status429TooManyRequests));

        MapMfaStatusEndpoint<TUser>(endpoints, builders);
    }

    private static void MapMfaStatusEndpoint<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapGet(
                        "/mfa/status",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                        {
                            var result = await mfaService.GetStatusAsync(httpContext.User, cancellationToken).ConfigureAwait(false);
                            return ToHttpResult(result);
                        })
                    .Produces<JwtMfaStatusResponse>(StatusCodes.Status200OK)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status401Unauthorized));
    }

    private static void MapMfaManagementEndpoints<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/authenticator/setup",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                        {
                            var (bodyOk, request) = await TryReadOptionalJsonAsync<JwtMfaAuthenticatorSetupRequest>(httpContext, cancellationToken)
                                .ConfigureAwait(false);
                            if (!bodyOk)
                            {
                                return CreateMalformedJsonProblem();
                            }

                            var result = await mfaService.SetupAuthenticatorAsync(httpContext.User, request, cancellationToken)
                                .ConfigureAwait(false);
                            return ToHttpResult(result);
                        })
                    .Produces<JwtMfaAuthenticatorSetupResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized));

        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/authenticator/confirm",
                        (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtMfaAuthenticatorConfirmRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await mfaService.ConfirmAuthenticatorAsync(httpContext.User, request, cancellationToken)
                                        .ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtMfaAuthenticatorConfirmResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType));

        MapSecondaryMethodEndpoints<TUser>(endpoints, builders);
        MapDisableEndpoint<TUser>(endpoints, builders);
    }

    private static void MapSecondaryMethodEndpoints<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/methods",
                        (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtMfaMethodUpdateRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await mfaService.UpdateMethodAsync(httpContext.User, request, cancellationToken)
                                        .ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtMfaMethodUpdateResponse>(StatusCodes.Status200OK)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status415UnsupportedMediaType));

        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/recovery-codes/regenerate",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                        {
                            var result = await mfaService.RegenerateRecoveryCodesAsync(httpContext.User, cancellationToken)
                                .ConfigureAwait(false);
                            return ToHttpResult(result);
                        })
                    .Produces<JwtMfaRecoveryCodesResponse>(StatusCodes.Status200OK)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden));
    }

    private static void MapDisableEndpoint<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/disable",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                        {
                            var (bodyOk, request) = await TryReadOptionalJsonAsync<JwtMfaDisableRequest>(httpContext, cancellationToken)
                                .ConfigureAwait(false);
                            if (!bodyOk)
                            {
                                return CreateMalformedJsonProblem();
                            }

                            var result = await mfaService.DisableMfaAsync(
                                    httpContext.User,
                                    request ?? new JwtMfaDisableRequest(),
                                    cancellationToken)
                                .ConfigureAwait(false);
                            return result.Succeeded ? Results.NoContent() : ToErrorResult(result);
                        })
                    .Produces(StatusCodes.Status204NoContent)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden));
    }

    private static void MapMfaSetupLinkEndpoints<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/setup-link",
                        async (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                        {
                            var (bodyOk, request) = await TryReadOptionalJsonAsync<JwtMfaSetupLinkRequest>(httpContext, cancellationToken)
                                .ConfigureAwait(false);
                            if (!bodyOk)
                            {
                                return CreateMalformedJsonProblem();
                            }

                            var result = await mfaService.SendSetupLinkAsync(httpContext.User, request, cancellationToken)
                                .ConfigureAwait(false);
                            return ToHttpResult(result);
                        })
                    .Produces<JwtMfaSetupLinkResponse>(StatusCodes.Status200OK)
                    .RequireAuthorization(BearerAuthorizeAttribute),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized));

        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/mfa/setup-link/exchange",
                        (
                            HttpContext httpContext,
                            IIdentityJwtMfaService<TUser> mfaService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtMfaSetupLinkExchangeRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await mfaService.ExchangeSetupLinkAsync(request, cancellationToken)
                                        .ConfigureAwait(false);
                                    return ToHttpResult(result);
                                }))
                    .Produces<JwtMfaSetupLinkExchangeResponse>(StatusCodes.Status200OK)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status415UnsupportedMediaType));
    }

    private static void MapRegistrationEndpoint<TUser>(
        IEndpointRouteBuilder endpoints,
        List<IEndpointConventionBuilder> builders)
        where TUser : class
    {
        builders.Add(
            WithProblemResponses(
                endpoints.MapPost(
                        "/register",
                        (
                            HttpContext httpContext,
                            IIdentityJwtAuthenticationService<TUser> authService,
                            CancellationToken cancellationToken) =>
                            ExecuteWithRequiredJsonAsync<JwtRegisterRequest>(
                                httpContext,
                                cancellationToken,
                                async request =>
                                {
                                    var result = await authService.RegisterAsync(request, cancellationToken).ConfigureAwait(false);
                                    return result.Succeeded
                                        ? Results.Json(result.Value, statusCode: StatusCodes.Status201Created)
                                        : ToErrorResult(result);
                                }))
                    .Produces<JwtRegisterResponse>(StatusCodes.Status201Created)
                    .AllowAnonymous(),
                StatusCodes.Status400BadRequest,
                StatusCodes.Status409Conflict,
                StatusCodes.Status415UnsupportedMediaType));
    }

    private static RouteHandlerBuilder WithProblemResponses(
        RouteHandlerBuilder builder,
        params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }

    private static Dictionary<string, object?> CreateErrorExtensions(string errorCode) =>
        new(StringComparer.Ordinal)
        {
            [IdentityJwtProblemDetailsDefaults.ErrorExtensionKey] = errorCode,
        };

    private static IResult CreateBadRequestJsonProblem(string detail) =>
        Results.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            extensions: CreateErrorExtensions("invalid_request"));

    private static IResult CreateMalformedJsonProblem() =>
        CreateBadRequestJsonProblem("Malformed JSON request body.");

    private static IResult ToHttpResult<T>(JwtServiceResult<T> result) =>
        result.Succeeded ? Results.Ok(result.Value) : ToErrorResult(result);

    private static IResult ToErrorResult<T>(JwtServiceResult<T> result)
    {
        var (statusCode, title, type) = result.Status switch
        {
            JwtOperationStatus.InvalidRequest => (StatusCodes.Status400BadRequest, "Bad Request", (string?)null),
            JwtOperationStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            JwtOperationStatus.LockedOut => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            JwtOperationStatus.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden", null),
            JwtOperationStatus.TooManyRequests => (
                StatusCodes.Status429TooManyRequests,
                "Too Many Requests",
                "https://tools.ietf.org/html/rfc6585#section-4"),
            JwtOperationStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict", null),
            _ => (StatusCodes.Status400BadRequest, "Bad Request", null),
        };

        var extensions = CreateErrorExtensions(result.ErrorCode ?? "error");
        if (result.Value is JwtMfaResendResponse { RetryAfterSeconds: > 0 } resend)
        {
            extensions[IdentityJwtProblemDetailsDefaults.RetryAfterSecondsExtensionKey] = resend.RetryAfterSeconds.Value;
        }

        return Results.Problem(
            detail: result.ErrorDescription,
            statusCode: statusCode,
            title: title,
            type: type,
            extensions: extensions);
    }

    private static async Task<IResult> ExecuteWithRequiredJsonAsync<TRequest>(
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Func<TRequest, Task<IResult>> handler)
        where TRequest : class
    {
        var (problem, request) = await ReadRequiredJsonAsync<TRequest>(httpContext, cancellationToken)
            .ConfigureAwait(false);
        if (problem is not null)
        {
            return problem;
        }

        var task = handler?.Invoke(request!);
        return task is null
            ? CreateMalformedJsonProblem()
            : await task.ConfigureAwait(false);
    }

    private static async Task<(IResult? Problem, T? Body)> ReadRequiredJsonAsync<T>(
        HttpContext httpContext,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!httpContext.Request.HasJsonContentType())
        {
            return (
                Results.Problem(
                    detail: "Expected an application/json request body.",
                    statusCode: StatusCodes.Status415UnsupportedMediaType,
                    title: "Unsupported Media Type",
                    extensions: CreateErrorExtensions("invalid_request")),
                null);
        }

        try
        {
            var model = await httpContext.Request.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            return model is null
                ? (CreateBadRequestJsonProblem("Request body is required."), null)
                : (null, model);
        }
        catch (JsonException)
        {
            return (CreateMalformedJsonProblem(), null);
        }
    }

    private static async Task<(bool Valid, T? Body)> TryReadOptionalJsonAsync<T>(
        HttpContext httpContext,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!httpContext.Request.HasJsonContentType() || httpContext.Request.ContentLength == 0)
        {
            return (true, null);
        }

        try
        {
            var model = await httpContext.Request.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            return (true, model);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }
}
