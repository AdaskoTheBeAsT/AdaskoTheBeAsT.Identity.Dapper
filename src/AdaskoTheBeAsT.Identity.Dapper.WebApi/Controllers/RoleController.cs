using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
[SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities", Justification = "CRUD actions for the Role resource intentionally share /api/Role.")]
public class RoleController : ControllerBase
{
    private readonly IWebApiMapper _mapper;
    private readonly IRequestHandler<CreateRoleRequest, IdentityResult> _createRoleHandler;
    private readonly IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>> _getAllRolesHandler;

    public RoleController(
        IWebApiMapper mapper,
        IRequestHandler<CreateRoleRequest, IdentityResult> createRoleHandler,
        IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>> getAllRolesHandler)
    {
        _mapper = mapper;
        _createRoleHandler = createRoleHandler;
        _getAllRolesHandler = getAllRolesHandler;
    }

    [HttpPost]
#pragma warning disable SEC0019 // Authentication uses explicit bearer headers, not automatically submitted cookie credentials.
    public async Task<IActionResult> CreateRoleAsync([FromBody] RoleModel roleModel)
#pragma warning restore SEC0019
    {
        try
        {
            var request = _mapper.MapToCreateRoleRequest(roleModel);
            var result = await _createRoleHandler.Handle(request, HttpContext.RequestAborted).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return Ok();
            }

            return ToIdentityValidationProblem(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Problem("Role creation failed.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRolesAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, IdentityPaging.MaxPageSize)] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetAllRolesRequest { Offset = offset, PageSize = pageSize };
            var roles = await _getAllRolesHandler.Handle(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return Ok(roles);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Problem("Role lookup failed.");
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
}
