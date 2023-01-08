namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IApplicationRoleStoreGenerator
{
    string Generate(
        string keyTypeName,
        string namespaceName);
}
