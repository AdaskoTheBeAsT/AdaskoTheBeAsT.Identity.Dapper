using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IApplicationRoleStoreGenerator
{
    string Generate(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        string keyTypeName,
        string namespaceName);
}
