using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserTokenClassGenerator
{
    string Generate(
        string schemaPart,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames);
}
