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
    private const string MethodClosingBrace = "        }";

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
        GenerateRolesProperty(sb);
        GenerateNormalizeSqlMethod(sb);
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
        GenerateAddClaimImpl(sb, keyTypeName, typePropertiesDict["IdentityRoleClaim"], options);
        GenerateRemoveClaimImpl(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private static void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            """
                    public ApplicationRoleStore(
                        IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
                        : base(
                            new IdentityErrorDescriber(),
                            connectionProvider,
                            new IdentityRoleSql(),
                            new IdentityRoleClaimSql())
                    {
                    }
            """);

        sb.AppendLine();
    }

    private static void GenerateRolesProperty(StringBuilder sb)
    {
        sb.AppendLine(
            """
                    public override IQueryable<ApplicationRole> Roles
                    {
                        get
                        {
                            ThrowIfDisposed();
                            using var connection = ConnectionProvider.Provide();
                            return connection.QueryIdentity<ApplicationRole>(NormalizeSql(IdentityRoleSql.GetRolesSql)).AsQueryable();
                        }
                    }
            """);

        sb.AppendLine();
    }

    private static void GenerateRoleParameters(StringBuilder sb, IdentityDapperOptions options, string concurrencyStamp)
    {
        sb.AppendLine(
            """            parameters.Add("Name", role.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                """            parameters.Add("NormalizedName", role.NormalizedName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");
        }

        sb.Append("            parameters.Add(\"ConcurrencyStamp\", ").Append(concurrencyStamp).AppendLine(", OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
    }

    private static void GenerateDeleteImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task DeleteImplAsync(
                        OracleConnection connection,
                        ApplicationRole role,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityRoleSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        parameters.Add("ConcurrencyStamp", role.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);

        sb.Append("            parameters.Add(\"Id\", role.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
                        if (affected != 1)
                        {
                            throw new DBConcurrencyException();
                        }
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }

    private static void GenerateFindByIdImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.Append("        protected override async Task<ApplicationRole?> FindByIdImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" roleId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityRoleSql.FindByIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", roleId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationRole>(sql, parameters, cancellationToken)
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }

    private static void GenerateFindByNameImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationRole?> FindByNameImplAsync(
                        OracleConnection connection,
                        string normalizedRoleName,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityRoleSql.FindByNameSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        sb.AppendLine(
            """            parameters.Add("NormalizedName", normalizedRoleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationRole>(sql, parameters, cancellationToken)
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }

    private static void GenerateGetClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.Append("        protected override async Task<IList<Claim>> GetClaimsImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" roleId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityRoleClaimSql.GetByRoleIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", roleId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false))
                            .AsList();
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }

    private static void GenerateAddClaimImpl(
        StringBuilder sb,
        string keyTypeName,
        IList<PropertyColumnTypeTriple> properties,
        IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override async Task AddClaimImplAsync(
                        OracleConnection connection,
                        ApplicationRoleClaim roleClaim,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityRoleClaimSql.CreateSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        OracleApplicationUserHelper.GenerateEntityParameters(sb, properties, "roleClaim", keyTypeName, options);

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }

    private static void GenerateRemoveClaimImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task RemoveClaimImplAsync(
                        OracleConnection connection,
                        ApplicationRoleClaim roleClaim,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityRoleClaimSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"RoleId\", roleClaim.RoleId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");\r\n            parameters.Add(\"ClaimType\", roleClaim.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"ClaimValue\", roleClaim.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine(MethodClosingBrace);
    }

    private void GenerateCreateImpl(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        StringBuilder sb,
        string keyTypeName,
        bool insertOwnId)
    {
        OracleStoreMethodGenerator.GenerateCreateStart(sb, "Role", "role", keyTypeName, insertOwnId);
        GenerateRoleParameters(sb, options, "role.ConcurrencyStamp");
        OracleStoreMethodGenerator.GenerateCustomParameters(
            sb, typePropertiesDict, nameof(IdentityRole<int>), _excludedProperties, "role", options);
        OracleStoreMethodGenerator.GenerateOutputId(sb, "role", keyTypeName);
    }

    private void GenerateUpdateImpl(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        StringBuilder sb,
        string keyTypeName)
    {
        OracleStoreMethodGenerator.GenerateUpdateStart(sb, "Role", "role", keyTypeName);
        GenerateRoleParameters(sb, options, "stamp");
        OracleStoreMethodGenerator.GenerateCustomParameters(
            sb, typePropertiesDict, nameof(IdentityRole<int>), _excludedProperties, "role", options);

        sb.AppendLine(
            """
                        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
                        if (affected != 1)
                        {
                            throw new DBConcurrencyException();
                        }

                        role.ConcurrencyStamp = stamp;
            """);

        sb.AppendLine(MethodClosingBrace);

        sb.AppendLine();
    }
}
