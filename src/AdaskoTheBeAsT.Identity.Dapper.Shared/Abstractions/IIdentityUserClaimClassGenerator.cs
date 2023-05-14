using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityUserClaimClassGenerator
{
    string Generate(
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs);
}
