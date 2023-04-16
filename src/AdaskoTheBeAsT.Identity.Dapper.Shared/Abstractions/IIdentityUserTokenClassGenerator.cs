using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserTokenClassGenerator
{
    string Generate(
        IdentityDapperConfiguration config,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames);
}
