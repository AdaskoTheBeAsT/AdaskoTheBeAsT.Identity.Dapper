using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteIdentityUserTokenClassGenerator
    : IdentityUserTokenClassGeneratorBase
{
    protected override string ProcessIdentityUserTokenCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}AspNetUserTokens(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserTokenDeleteSql(IdentityDapperConfiguration config)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserToken<int>.LoginProvider)}=@{nameof(IdentityUserToken<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserToken<int>.Name)}=@{nameof(IdentityUserToken<int>.Name)}")
            .Where2($"{nameof(IdentityUserToken<int>.Value)}=@{nameof(IdentityUserToken<int>.Value)}")
            .Where2($"{nameof(IdentityUserToken<int>.UserId)}=@{nameof(IdentityUserToken<int>.UserId)}")
            .AddTemplate(
                $"DELETE FROM {config.SchemaPart}AspNetUserTokens\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserTokenGetByUserIdSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount);
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"[{columnNames[i]}] AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUserToken<int>.UserId)}=@{nameof(IdentityUserToken<int>.UserId)}")
            .Where2($"{nameof(IdentityUserToken<int>.LoginProvider)}=@{nameof(IdentityUserToken<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserToken<int>.Name)}=@{nameof(IdentityUserToken<int>.Name)}")
            .AddTemplate(
                $"SELECT TOP 1 /**select2**/FROM {config.SchemaPart}AspNetUserTokens\r\n/**where2**/;")
            .RawSql;
    }
}
