using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;

public interface IWebApiMapper
{
    AuthPasswordRequest MapToPasswordRequest(AuthenticationModel model);

    AuthRefreshTokenRequest MapToRefreshTokenRequest(AuthenticationModel model);

    CreateUserRequest MapToCreateUserRequest(UserModel model);

    UpdateUserRequest MapToUpdateUserRequest(UpdateUserModel model);

    UserModel MapToUserModel(
        ApplicationUser user,
        ICollection<string> roles);

    CreateRoleRequest MapToCreateRoleRequest(RoleModel model);

    IReadOnlyList<RoleModel> MapToRoleModels(IEnumerable<ApplicationRole> roles);
}
