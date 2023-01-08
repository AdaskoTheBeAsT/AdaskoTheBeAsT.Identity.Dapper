using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityRoleClassGenerator
{
    string Generate(
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames);
}