namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IApplicationUserOnlyStoreGenerator
{
    string Generate(
        string keyTypeName,
        string namespaceName);
}
