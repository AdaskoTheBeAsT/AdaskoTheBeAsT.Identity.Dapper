using System;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TokenController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost("token")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TokenAsync([FromForm] AuthRequestBase authRequest)
        {
            try
            {
                var result = await _mediator.Send(authRequest).ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
