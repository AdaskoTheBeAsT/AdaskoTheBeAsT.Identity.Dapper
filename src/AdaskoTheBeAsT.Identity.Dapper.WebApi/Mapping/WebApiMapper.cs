using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;

public sealed class WebApiMapper : IWebApiMapper
{
    public AuthPasswordRequest MapToPasswordRequest(AuthenticationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AuthPasswordRequest
        {
            Username = model.Username,
            Password = model.Password,
        };
    }

    public AuthRefreshTokenRequest MapToRefreshTokenRequest(AuthenticationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new AuthRefreshTokenRequest
        {
            RefreshToken = model.RefreshToken,
        };
    }

    public CreateUserRequest MapToCreateUserRequest(UserModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new CreateUserRequest
        {
            UserName = model.UserName,
            Password = model.Password,
            Roles = model.Roles != null ? new List<string>(model.Roles) : new List<string>(),
        };
    }

    public UpdateUserRequest MapToUpdateUserRequest(UpdateUserModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new UpdateUserRequest
        {
            UserName = model.UserName,
            Roles = model.Roles != null ? new List<string>(model.Roles) : null,
        };
    }

    public UserModel MapToUserModel(
        ApplicationUser user,
        ICollection<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        return new UserModel
        {
            UserName = user.UserName,
            Roles = roles,
        };
    }

    public CreateRoleRequest MapToCreateRoleRequest(RoleModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new CreateRoleRequest
        {
            Name = model.Name,
        };
    }

    public IReadOnlyList<RoleModel> MapToRoleModels(IEnumerable<ApplicationRole> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        return roles
            .Select(role => new RoleModel { Name = role.Name })
            .ToArray();
    }
}
