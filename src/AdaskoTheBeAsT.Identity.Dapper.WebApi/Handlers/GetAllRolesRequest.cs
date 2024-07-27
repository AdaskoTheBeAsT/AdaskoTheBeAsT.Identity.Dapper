using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using MediatR;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public class GetAllRolesRequest
    : IRequest<IEnumerable<RoleModel>>
{
}
