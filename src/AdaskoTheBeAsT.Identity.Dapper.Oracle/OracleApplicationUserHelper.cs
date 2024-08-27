using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleApplicationUserHelper
{
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
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

    public static void GenerateCreateImpl(
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
            foreach (var item in properties.Where(e => !ExcludedProperties.Contains(e.PropertyName)))
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

        sb.AppendLine();
    }

    public static void GenerateUpdateImpl(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task UpdateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.UpdateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        
        sb.AppendLine(
            $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

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
            foreach (var item in properties.Where(e => !ExcludedProperties.Contains(e.PropertyName)))
            {
                sb.AppendLine(
                    $@"            parameters.Add(""{item.ColumnName}"", user.{item.PropertyName}, {OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs)}");
            }
        }

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateDeleteImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task DeleteImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);

        sb.AppendLine(
            $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByIdImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUser?> FindByIdImplAsync(
            OracleConnection connection,
            {keyTypeName} userId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", userId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByNameImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUser?> FindByNameImplAsync(
            OracleConnection connection,
            string normalizedUserName,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.FindByNameSql;
            var parameters = new OracleDynamicParameters();");

        sb.AppendLine(
            $@"            parameters.Add(""NormalizedName"", normalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<Claim>> GetClaimsImplAsync(
            OracleConnection connection,
            {keyTypeName} userId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserClaimSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", userId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<Claim>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateAddClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task AddClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            IEnumerable<Claim> claims,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserClaimSql.CreateSql;");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine($@"            foreach (var claim in claims)
            {{");
        sb.AppendLine(
            $@"                var parameters = new OracleDynamicParameters();
                parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
                parameters.Add(""ClaimType"", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add(""ClaimValue"", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"                await connection.ExecuteAsync(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("            }");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateReplaceClaimImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task ReplaceClaimImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            Claim claim,
            Claim newClaim,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserClaimSql.ReplaceSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""ClaimTypeOld"", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimValueOld"", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimTypeNew"", newClaim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimValueNew"", newClaim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateRemoveClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task RemoveClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            IEnumerable<Claim> claims,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserClaimSql.DeleteSql;
            foreach (var claim in claims)
            {{
                var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"                parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
                parameters.Add(""ClaimType"", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add(""ClaimValue"", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"                await connection.ExecuteAsync(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("            }");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateAddLoginImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task AddLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            UserLoginInfo login,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserLoginSql.CreateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""LoginProvider"", login.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""ProviderKey"", login.ProviderKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""ProviderDisplayName"", login.ProviderDisplayName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateRemoveLoginImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task RemoveLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserLoginSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""LoginProvider"", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""ProviderKey"", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetLoginsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<UserLoginInfo>> GetLoginsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserLoginSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<UserLoginInfo>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUser?> FindUserImplAsync(
            OracleConnection connection,
            {keyTypeName} userId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", userId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserLoginImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
            OracleConnection connection,
            {keyTypeName} userId,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserLoginSql.GetByUserIdLoginProviderKeySql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", userId, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""LoginProvider"", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""ProviderKey"", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserLoginImpl2(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
            OracleConnection connection,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserLoginSql.GetByLoginProviderKeySql;
            var parameters = new OracleDynamicParameters();
            parameters.Add(""LoginProvider"", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""ProviderKey"", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByEmailImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUser?> FindByEmailImplAsync(
            OracleConnection connection,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.FindByEmailSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add(""NormalizedEmail"", normalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetUsersForClaimImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<ApplicationUser>> GetUsersForClaimImplAsync(
            OracleConnection connection,
            Claim claim,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.GetUsersForClaimSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add(""ClaimType"", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add(""ClaimValue"", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<ApplicationUser>(sql, parameters)
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindTokenImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUserToken?> FindTokenImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            string loginProvider,
            string name,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserTokenSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""LoginProvider"", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""Name"", name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUserToken>(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateAddUserTokenImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task AddUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserTokenSql.CreateSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", token.UserId, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""LoginProvider"", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""Name"", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""Value"", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateRemoveUserTokenImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task RemoveUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserTokenSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""LoginProvider"", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""Name"", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""Value"", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add(""UserId"", token.UserId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }
}
