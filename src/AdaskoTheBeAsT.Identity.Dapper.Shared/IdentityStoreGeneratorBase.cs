using System.Collections.Generic;
using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityStoreGeneratorBase
    : IdentityClassGeneratorBase
{
    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) => [];

    protected override void GenerateUsing(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine("using Microsoft.AspNetCore.Identity;");
        sb.AppendLine();
    }
}
