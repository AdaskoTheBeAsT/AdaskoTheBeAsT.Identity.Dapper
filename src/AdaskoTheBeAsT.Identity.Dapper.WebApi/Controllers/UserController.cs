using System;
using System.Threading.Tasks;
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
    public class UserController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public UserController(
            IMapper mapper,
            IMediator mediator)
        {
            _mapper = mapper;
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserModel userModel)
        {
            try
            {
                var request = _mapper.Map<CreateUserRequest>(userModel);
                await _mediator.Send(request).ConfigureAwait(false);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
