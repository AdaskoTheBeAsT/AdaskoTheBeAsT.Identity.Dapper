using System;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers
{
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
        public async Task<IActionResult> TokenAsync([FromForm] AuthenticationModel model)
        {
            try
            {
                var request = Map(model);
                var result = await _mediator.Send(request).ConfigureAwait(false);
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
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        private AuthRequestBase Map(AuthenticationModel model) =>
            model?.GrantType switch
            {
                GrantType.ClientCredentials => _mapper.Map<AuthClientCredentialRequest>(model),
                GrantType.Password => _mapper.Map<AuthPasswordRequest>(model),
                GrantType.RefreshToken => _mapper.Map<AuthRefreshTokenRequest>(model),
                _ => throw new InvalidGrantTypeException(),
            };
    }
}
