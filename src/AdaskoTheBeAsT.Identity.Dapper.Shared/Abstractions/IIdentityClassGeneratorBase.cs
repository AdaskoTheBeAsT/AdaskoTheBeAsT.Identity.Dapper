using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityClassGeneratorBase
{
    IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId);
}
