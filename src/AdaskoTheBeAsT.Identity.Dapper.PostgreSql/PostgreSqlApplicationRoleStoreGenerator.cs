using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlApplicationRoleStoreGenerator
    : PostgreSqlIdentityStoreGeneratorBase,
        IApplicationRoleStoreGenerator
{
    public string Generate(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool insertOwnId)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, keyTypeName);
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(
            sb,
            "ApplicationRoleStore",
            $"DapperRoleStoreBase<ApplicationRole, {keyTypeName}, ApplicationRoleClaim, NpgsqlConnection>");
        GenerateConstructor(sb);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationRoleStore(
            IIdentityDbConnectionProvider<NpgsqlConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }");
    }
}
