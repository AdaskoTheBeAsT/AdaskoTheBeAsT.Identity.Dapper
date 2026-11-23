using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IWebApiMapper _mapper;
    private readonly IRequestHandler<CreateUserRequest, IdentityResult> _createHandler;
    private readonly IRequestHandler<GetUserByIdRequest, UserModel?> _getByIdHandler;
    private readonly IRequestHandler<UpdateUserRequest, IdentityResult> _updateHandler;
    private readonly IRequestHandler<DeleteUserRequest, IdentityResult> _deleteHandler;

    public UserController(
        IWebApiMapper mapper,
        IRequestHandler<CreateUserRequest, IdentityResult> createHandler,
        IRequestHandler<GetUserByIdRequest, UserModel?> getByIdHandler,
        IRequestHandler<UpdateUserRequest, IdentityResult> updateHandler,
        IRequestHandler<DeleteUserRequest, IdentityResult> deleteHandler)
    {
        _mapper = mapper;
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateUserAsync([FromBody] UserModel userModel)
    {
        if (userModel.Roles is { Count: > 0 })
        {
            return Problem(
                detail: "Roles cannot be assigned during public registration.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        try
        {
            var request = _mapper.MapToCreateUserRequest(userModel);
            var result = await _createHandler.Handle(request, HttpContext.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);
            return result.Succeeded ? Ok() : ToIdentityValidationProblem(result);
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
            var user = await _getByIdHandler.Handle(request, HttpContext.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);
            if (user == null)
            {
                return Problem(
                    detail: "User was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
            }

            return Ok(user);
        }
        catch (Exception)
        {
            return Problem("User lookup failed.");
        }
    }

    [HttpPut("{id}")]
#pragma warning disable SEC0019 // Authentication uses explicit bearer headers, not automatically submitted cookie credentials.
    public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] UpdateUserModel updateUserModel)
#pragma warning restore SEC0019
    {
        if (!CanManageUser(id) || (updateUserModel.Roles != null && !User.IsInRole("Administrator")))
        {
            return Forbid();
        }

        try
        {
            var request = _mapper.MapToUpdateUserRequest(updateUserModel);
            request.UserId = id;
            var result = await _updateHandler.Handle(request, HttpContext.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return ToIdentityValidationProblem(result);
        }
        catch (Exception)
        {
            return Problem("User update failed.");
        }
    }

    [HttpDelete("{id}")]
#pragma warning disable SEC0019 // Authentication uses explicit bearer headers, not automatically submitted cookie credentials.
    public async Task<IActionResult> DeleteUserAsync(Guid id)
#pragma warning restore SEC0019
    {
        if (!CanManageUser(id))
        {
            return Forbid();
        }

        try
        {
            var request = new DeleteUserRequest { UserId = id };
            var result = await _deleteHandler.Handle(request, HttpContext.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return ToIdentityValidationProblem(result);
        }
        catch (Exception)
        {
            return Problem("User deletion failed.");
        }
    }

    private ActionResult ToIdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Code) ? "identity" : error.Code;
            ModelState.AddModelError(key, error.Description);
        }

        return ValidationProblem(ModelState);
    }

    private bool CanManageUser(Guid id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (User.IsInRole("Administrator"))
        {
            return true;
        }

        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out var currentId) && currentId == id;
    }
}
