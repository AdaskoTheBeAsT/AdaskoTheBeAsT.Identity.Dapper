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

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateRoleAsync([FromBody] RoleModel roleModel)
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

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAllRolesAsync()
    {
        try
        {
            var request = new GetAllRolesRequest();
            var roles = await _mediator.Send(request).ConfigureAwait(continueOnCapturedContext: false);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
