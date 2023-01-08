using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface IIdentityHelper
{
    ISet<string> GuidNameSet { get; }

    string GetInsertTemplate(string tableName, string keyTypeName);
}
