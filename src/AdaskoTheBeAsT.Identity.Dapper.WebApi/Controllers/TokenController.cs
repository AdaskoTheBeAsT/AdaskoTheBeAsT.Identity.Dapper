using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController : ControllerBase
{
    private readonly IWebApiMapper _mapper;
    private readonly IRequestValidator<AuthPasswordRequest> _passwordValidator;
    private readonly IRequestHandler<AuthPasswordRequest, Token> _passwordHandler;
    private readonly IRequestHandler<AuthRefreshTokenRequest, Token> _refreshHandler;

    public TokenController(
        IWebApiMapper mapper,
        IRequestValidator<AuthPasswordRequest> passwordValidator,
        IRequestHandler<AuthPasswordRequest, Token> passwordHandler,
        IRequestHandler<AuthRefreshTokenRequest, Token> refreshHandler)
    {
        _mapper = mapper;
        _passwordValidator = passwordValidator;
        _passwordHandler = passwordHandler;
        _refreshHandler = refreshHandler;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> TokenAsync(
        [FromForm] AuthenticationModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteGrantAsync(model, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            return Ok(result);
        }
        catch (UserNotFoundException)
        {
            return Unauthorized();
        }
        catch (InvalidPasswordException)
        {
            return Unauthorized();
        }
        catch (InvalidRefreshTokenException)
        {
            return Unauthorized();
        }
        catch (InvalidGrantTypeException)
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }
        catch (RequestValidationException)
        {
            return BadRequest(new { error = "invalid_request" });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Problem(statusCode: 500, title: "Token issuance failed.");
        }
    }

    private Task<Token> ExecuteGrantAsync(
        AuthenticationModel? model,
        CancellationToken cancellationToken)
    {
        if (model == null)
        {
            throw new InvalidGrantTypeException();
        }

        return model.GrantType switch
        {
            GrantType.Password => ExecutePasswordGrantAsync(model, cancellationToken),
            GrantType.RefreshToken => _refreshHandler.Handle(_mapper.MapToRefreshTokenRequest(model), cancellationToken),
            _ => throw new InvalidGrantTypeException(),
        };
    }

    private Task<Token> ExecutePasswordGrantAsync(
        AuthenticationModel model,
        CancellationToken cancellationToken)
    {
        var request = _mapper.MapToPasswordRequest(model);
        _passwordValidator.ValidateAndThrow(request);
        return _passwordHandler.Handle(request, cancellationToken);
    }
}
