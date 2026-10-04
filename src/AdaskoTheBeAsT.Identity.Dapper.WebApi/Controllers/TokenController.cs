using System;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public TokenController(
        IMapper mapper,
        IMediator mediator)
    {
        _mapper = mapper;
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> TokenAsync([FromForm] AuthenticationModel model, CancellationToken cancellationToken)
    {
        try
        {
            var request = Map(model);
            var result = await _mediator.Send(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
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
        catch (ValidationException)
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

    private AuthRequestBase Map(AuthenticationModel model) =>
        model?.GrantType switch
        {
            GrantType.Password => _mapper.Map<AuthPasswordRequest>(model),
            GrantType.RefreshToken => _mapper.Map<AuthRefreshTokenRequest>(model),
            _ => throw new InvalidGrantTypeException(),
        };
}
