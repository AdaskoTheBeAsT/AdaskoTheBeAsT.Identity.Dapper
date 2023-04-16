using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityHelper
{
    ISet<string> GuidNameSet { get; }

    ISet<string> NumberNameSet { get; }

    ISet<string> StringNameSet { get; }

    string GetInsertTemplate(
        string tableName,
        string keyTypeName);
}
