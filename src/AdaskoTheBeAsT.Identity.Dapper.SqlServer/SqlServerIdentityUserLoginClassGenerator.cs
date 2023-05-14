using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityUserLoginClassGenerator
    : IdentityUserLoginClassGeneratorBase
{
    protected override string ProcessIdentityUserLoginCreateSql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnPairs.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", propertyColumnPairs.Select(s => $"@{s.PropertyName}")))
            .AddTemplate(
                $"INSERT INTO {schemaPart}AspNetUserLogins(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginDeleteSql(string schemaPart)
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .Where2($"{nameof(IdentityUserLogin<int>.UserId)}=@{nameof(IdentityUserLogin<int>.UserId)}")
            .AddTemplate(
                $"DELETE FROM {schemaPart}AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByUserIdSql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnPairs.Count);
        foreach (var localPair in propertyColumnPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"UserId=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {schemaPart}AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnPairs.Count);
        foreach (var localPair in propertyColumnPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"UserId=@{nameof(IdentityUser.Id)}")
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {schemaPart}AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnPairs.Count);
        foreach (var localPair in propertyColumnPairs)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .AddTemplate(
                $"SELECT /**select2**/FROM {schemaPart}AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }
}
