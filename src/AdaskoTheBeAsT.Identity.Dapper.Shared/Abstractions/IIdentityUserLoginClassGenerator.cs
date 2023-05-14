using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserLoginClassGenerator
{
    string Generate(
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs);
}
