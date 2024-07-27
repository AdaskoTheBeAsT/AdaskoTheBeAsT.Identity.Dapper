using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;

public class AutoMapperProfile
    : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<AuthenticationModel, AuthClientCredentialRequest>(MemberList.Destination);
        CreateMap<AuthenticationModel, AuthPasswordRequest>(MemberList.Destination);
        CreateMap<AuthenticationModel, AuthRefreshTokenRequest>(MemberList.Destination);
        CreateMap<UserModel, CreateUserRequest>(MemberList.Destination);
        CreateMap<UpdateUserModel, UpdateUserRequest>(MemberList.Destination);
        CreateMap<ApplicationUser, UserModel>(MemberList.Destination);
        CreateMap<RoleModel, CreateRoleRequest>(MemberList.Destination);
        CreateMap<ApplicationRole, RoleModel>(MemberList.Destination);
    }
}
