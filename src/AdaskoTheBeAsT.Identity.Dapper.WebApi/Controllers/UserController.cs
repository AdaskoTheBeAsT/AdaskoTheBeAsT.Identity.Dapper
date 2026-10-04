using System;
using System.Security.Claims;
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
[Authorize]
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
        if (userModel.Roles is { Count: > 0 })
        {
            return BadRequest("Roles cannot be assigned during public registration.");
        }

        try
        {
            var request = _mapper.Map<CreateUserRequest>(userModel);
            var result = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }
        catch (Exception)
        {
            return Problem("User creation failed.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(Guid id)
    {
        if (!CanManageUser(id))
        {
            return Forbid();
        }

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
        catch (Exception)
        {
            return Problem("User lookup failed.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] UpdateUserModel updateUserModel)
    {
        if (!CanManageUser(id) || (updateUserModel.Roles != null && !User.IsInRole("Administrator")))
        {
            return Forbid();
        }

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
        catch (Exception)
        {
            return Problem("User update failed.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        if (!CanManageUser(id))
        {
            return Forbid();
        }

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
        catch (Exception)
        {
            return Problem("User deletion failed.");
        }
    }

    private bool CanManageUser(Guid id) =>
        User.Identity?.IsAuthenticated == true &&
        (User.IsInRole("Administrator") ||
         (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId == id));
}
