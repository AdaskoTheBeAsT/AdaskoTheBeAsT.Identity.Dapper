using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
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
[Authorize(Roles = "Administrator")]
public class RoleController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public RoleController(
        IMapper mapper,
        IMediator mediator)
    {
        _mapper = mapper;
        _mediator = mediator;
    }

    [HttpPost]
#pragma warning disable SEC0019 // Authentication uses explicit bearer headers, not automatically submitted cookie credentials.
    public async Task<IActionResult> CreateRoleAsync([FromBody] RoleModel roleModel)
#pragma warning restore SEC0019
    {
        try
        {
            var request = _mapper.Map<CreateRoleRequest>(roleModel);
            var result = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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
            var roles = await _mediator.Send(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return Ok(roles);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
