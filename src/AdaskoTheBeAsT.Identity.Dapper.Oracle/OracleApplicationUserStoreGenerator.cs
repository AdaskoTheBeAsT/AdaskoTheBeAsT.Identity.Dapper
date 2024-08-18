using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleApplicationUserStoreGenerator
    : OracleIdentityStoreGeneratorBase,
        IApplicationUserStoreGenerator
{
    private readonly HashSet<string> _excludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IdentityUser<int>.Id),
        nameof(IdentityUser<int>.UserName),
        nameof(IdentityUser<int>.NormalizedUserName),
        nameof(IdentityUser<int>.Email),
        nameof(IdentityUser<int>.NormalizedEmail),
        nameof(IdentityUser<int>.EmailConfirmed),
        nameof(IdentityUser<int>.PasswordHash),
        nameof(IdentityUser<int>.SecurityStamp),
        nameof(IdentityUser<int>.ConcurrencyStamp),
        nameof(IdentityUser<int>.PhoneNumber),
        nameof(IdentityUser<int>.PhoneNumberConfirmed),
        nameof(IdentityUser<int>.TwoFactorEnabled),
        nameof(IdentityUser<int>.LockoutEnd),
        nameof(IdentityUser<int>.LockoutEnabled),
        nameof(IdentityUser<int>.AccessFailedCount),
    };

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
            "ApplicationUserStore",
            $"DapperUserStoreBase<ApplicationUser, ApplicationRole, {keyTypeName}, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationUserToken, OracleConnection>");
        GenerateConstructor(sb);
        GenerateCreateImpl(
            typePropertiesDict,
            options,
            sb,
            keyTypeName,
            insertOwnId);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationUserStore(
            IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql(),
                new IdentityUserRoleSql(),
                new IdentityRoleSql(),
                new IdentityUserRoleClaimSql())
        {
        }");
        sb.AppendLine();
    }

    private void GenerateCreateImpl(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        StringBuilder sb,
        string keyTypeName,
        bool insertOwnId)
    {
        sb.AppendLine(
            $@"        protected override async Task CreateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.CreateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""OutputId"", dbType: {idType}, direction: ParameterDirection.ReturnValue, size: {idSize});");

        if (insertOwnId)
        {
            sb.AppendLine(
                $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");
        }

        sb.AppendLine(
            $@"            parameters.Add(""UserName"", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                $@"            parameters.Add(""NormalizedUserName"", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
        }

        sb.AppendLine(
            $@"            parameters.Add(""Email"", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                $@"            parameters.Add(""NormalizedEmail"", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
        }

        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($@"            parameters.Add(""EmailConfirmed"", user.EmailConfirmed, {OracleTypeMapper.MapParameterEndByStoreBooleanAs(options.StoreBooleanAs)}");
        }

        sb.AppendLine(
            $@"            parameters.Add(""PasswordHash"", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""SecurityStamp"", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ConcurrencyStamp"", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""PhoneNumber"", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($@"            parameters.Add(""PhoneNumberConfirmed"", user.PhoneNumberConfirmed, {OracleTypeMapper.MapParameterEndByStoreBooleanAs(options.StoreBooleanAs)}");
            sb.AppendLine($@"            parameters.Add(""TwoFactorEnabled"", user.TwoFactorEnabled, {OracleTypeMapper.MapParameterEndByStoreBooleanAs(options.StoreBooleanAs)}");
        }

        sb.AppendLine(
            $@"            parameters.Add(""LockoutEnd"", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);");

        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($@"            parameters.Add(""LockoutEnabled"", user.LockoutEnabled, {OracleTypeMapper.MapParameterEndByStoreBooleanAs(options.StoreBooleanAs)}");
        }

        sb.AppendLine(
            $@"            parameters.Add(""AccessFailedCount"", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);");

        if (typePropertiesDict.TryGetValue(nameof(IdentityUser<int>), out var properties))
        {
            foreach (var item in properties.Where(e => !_excludedProperties.Contains(e.PropertyName)))
            {
                sb.AppendLine(
                    $@"            parameters.Add(""{item.ColumnName}"", user.{item.PropertyName}, {OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs)}");
            }
        }

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        if (string.Equals(keyTypeName, "string", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "int", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "long", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $@"            user.Id = parameters.Get<{keyTypeName}>(""OutputId"");");
        }
        else if (string.Equals(keyTypeName, "Guid", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $@"            var idBytes = parameters.Get<byte[]>(""OutputId"");
            user.Id = new Guid(idBytes);");
        }

        sb.AppendLine("        }");
    }
}
