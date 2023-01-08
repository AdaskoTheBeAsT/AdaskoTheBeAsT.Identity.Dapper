namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IApplicationUserStoreGenerator
{
    string Generate(
        string keyTypeName,
        string namespaceName);
}
