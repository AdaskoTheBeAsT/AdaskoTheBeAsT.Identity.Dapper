using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleApplicationRoleStoreGenerator
    : OracleIdentityStoreGeneratorBase,
        IApplicationRoleStoreGenerator
{
    private readonly HashSet<string> _excludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IdentityRole<int>.Id),
        nameof(IdentityRole<int>.Name),
        nameof(IdentityRole<int>.NormalizedName),
        nameof(IdentityRole<int>.ConcurrencyStamp),
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
            "ApplicationRoleStore",
            $"DapperRoleStoreBase<ApplicationRole, {keyTypeName}, ApplicationRoleClaim, OracleConnection>");
        GenerateConstructor(sb);
        GenerateCreateImpl(
            typePropertiesDict,
            options,
            sb,
            keyTypeName,
            insertOwnId);
        GenerateUpdateImpl(
            typePropertiesDict,
            options,
            sb,
            keyTypeName);
        GenerateDeleteImpl(sb, keyTypeName);
        GenerateFindByIdImpl(sb, keyTypeName);
        GenerateFindByNameImpl(sb);
        GenerateGetClaimsImpl(sb, keyTypeName);
        GenerateAddClaimImpl(sb, keyTypeName);
        GenerateRemoveClaimImpl(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationRoleStore(
            IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
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
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.CreateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""OutputId"", dbType: {idType}, direction: ParameterDirection.ReturnValue, size: {idSize});");

        if (insertOwnId)
        {
            sb.AppendLine(
                $@"            parameters.Add(""Id"", role.Id, {idType}, ParameterDirection.Input, {idSize});");
        }

        sb.AppendLine(
            $@"            parameters.Add(""Name"", role.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                $@"            parameters.Add(""NormalizedName"", role.NormalizedName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
        }

        sb.AppendLine(
            $@"            parameters.Add(""ConcurrencyStamp"", role.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (typePropertiesDict.TryGetValue(nameof(IdentityRole<int>), out var properties))
        {
            foreach (var item in properties.Where(e => !_excludedProperties.Contains(e.PropertyName)))
            {
                sb.AppendLine(
                    $@"            parameters.Add(""{item.ColumnName}"", role.{item.PropertyName}, {OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs)}");
            }
        }

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        if (string.Equals(keyTypeName, "string", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "int", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "long", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $@"            role.Id = parameters.Get<{keyTypeName}>(""OutputId"");");
        }
        else if (string.Equals(keyTypeName, "Guid", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $@"            var idBytes = parameters.Get<byte[]>(""OutputId"");
            role.Id = new Guid(idBytes);");
        }

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateUpdateImpl(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task UpdateImplAsync(
            OracleConnection connection,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.UpdateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);

        sb.AppendLine(
            $@"            parameters.Add(""Id"", role.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            parameters.Add(""Name"", role.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                $@"            parameters.Add(""NormalizedName"", role.NormalizedName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
        }

        sb.AppendLine(
            $@"            parameters.Add(""ConcurrencyStamp"", role.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        if (typePropertiesDict.TryGetValue(nameof(IdentityRole<int>), out var properties))
        {
            foreach (var item in properties.Where(e => !_excludedProperties.Contains(e.PropertyName)))
            {
                sb.AppendLine(
                    $@"            parameters.Add(""{item.ColumnName}"", role.{item.PropertyName}, {OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs)}");
            }
        }

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateDeleteImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task DeleteImplAsync(
            OracleConnection connection,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);

        sb.AppendLine(
            $@"            parameters.Add(""Id"", role.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateFindByIdImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationRole> FindByIdImplAsync(
            OracleConnection connection,
            {keyTypeName} roleId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", roleId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationRole>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateFindByNameImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationRole> FindByNameImplAsync(
            OracleConnection connection,
            string normalizedRoleName,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.FindByNameSql;
            var parameters = new OracleDynamicParameters();");

        sb.AppendLine(
            $@"            parameters.Add(""NormalizedName"", normalizedRoleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
        
        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationRole>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateGetClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<Claim>> GetClaimsImplAsync(
            OracleConnection connection,
            {keyTypeName} roleId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleClaimSql.GetByRoleIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", roleId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<Claim>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateAddClaimImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task AddClaimImplAsync(
            OracleConnection connection,
            ApplicationRoleClaim roleClaim,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleClaimSql.CreateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""RoleId"", roleClaim.RoleId, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""ClaimType"", roleClaim.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimValue"", roleClaim.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateRemoveClaimImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task RemoveClaimImplAsync(
            OracleConnection connection,
            ApplicationRoleClaim roleClaim,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleClaimSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""RoleId"", roleClaim.RoleId, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""ClaimType"", roleClaim.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimValue"", roleClaim.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");
    }
}
