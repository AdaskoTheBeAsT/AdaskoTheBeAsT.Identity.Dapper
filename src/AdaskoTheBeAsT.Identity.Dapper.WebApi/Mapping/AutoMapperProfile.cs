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
        CreateMap<UpdateUserModel, UpdateUserRequest>(MemberList.Destination)
            .ForMember(d => d.Roles, o => o.AllowNull())
            .ForMember(d => d.UserId, o => o.Ignore());
        CreateMap<ApplicationUser, UserModel>(MemberList.Destination)
            .ForMember(d => d.Password, o => o.Ignore())
            .ForMember(d => d.Roles, o => o.Ignore());
        CreateMap<RoleModel, CreateRoleRequest>(MemberList.Destination);
        CreateMap<ApplicationRole, RoleModel>(MemberList.Destination);
    }
}
