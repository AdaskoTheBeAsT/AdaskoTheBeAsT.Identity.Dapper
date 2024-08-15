using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserLoginClassGenerator
    : IIdentityClassGeneratorBase
{
    string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);
}
