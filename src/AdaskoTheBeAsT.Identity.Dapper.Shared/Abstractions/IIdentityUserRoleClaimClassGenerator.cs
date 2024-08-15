namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserRoleClaimClassGenerator
    : IIdentityClassGeneratorBase
{
    string Generate(IdentityDapperConfiguration config);
}
