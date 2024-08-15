using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IApplicationUserStoreGenerator
{
    string Generate(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        string keyTypeName,
        string namespaceName);
}
