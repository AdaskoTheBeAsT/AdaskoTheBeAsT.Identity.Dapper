using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleApplicationUserOnlyStoreGenerator
    : IdentityStoreGeneratorBase,
        IApplicationUserOnlyStoreGenerator
{
    public string Generate(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        string keyTypeName,
        string namespaceName)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, keyTypeName);
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(
            sb,
            "ApplicationUserOnlyStore",
            $"DapperUserOnlyStoreBase<ApplicationUser, {keyTypeName}, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken>");
        GenerateConstructor(sb);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected override void GenerateUsing(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine("using Dapper.Oracle;");
        sb.AppendLine("using Microsoft.AspNetCore.Identity;");
        sb.AppendLine("using Oracle.ManagedDataAccess.Client;");
        sb.AppendLine();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationUserOnlyStore(
            IIdentityDbConnectionProvider connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql())
        {
        }");
    }
}
