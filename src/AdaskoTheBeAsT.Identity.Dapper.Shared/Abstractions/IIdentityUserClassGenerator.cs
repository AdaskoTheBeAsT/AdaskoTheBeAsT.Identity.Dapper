using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserClassGenerator
{
    string Generate(
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> propertyColumnTypeTriples);
}
