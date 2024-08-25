using System;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

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
            await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(Guid id)
    {
        try
        {
            var request = new GetUserByIdRequest { UserId = id };
            var user = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] UpdateUserModel updateUserModel)
    {
        try
        {
            var request = _mapper.Map<UpdateUserRequest>(updateUserModel);
            request.UserId = id;
            var result = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        try
        {
            var request = new DeleteUserRequest { UserId = id };
            var result = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
