using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

public interface ISourceGeneratorHelper
{
    void GenerateCode(
        SourceProductionContext context,
        string dbSchema,
        (string KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) generationInfo);
}
