using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public interface ISourceGeneratorHelper
{
    void GenerateCode(
        SourceProductionContext context,
        (string? KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) generationInfo);
}
