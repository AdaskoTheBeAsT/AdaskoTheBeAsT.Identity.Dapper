namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserRoleClaimClassGenerator
{
    string Generate(IdentityDapperConfiguration config);
}
