using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

public class SqlIdentityUserLoginClassGenerator
    : IdentityUserLoginClassGeneratorBase
{
    protected override string ProcessIdentityUserLoginCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO dbo.AspNetUserLogins(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginDeleteSql()
    {
        var sqlBuilder = new AdvancedSqlBuilder();

        return sqlBuilder
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .Where2($"{nameof(IdentityUserLogin<int>.UserId)}=@{nameof(IdentityUserLogin<int>.UserId)}")
            .AddTemplate(
                "DELETE FROM dbo.AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByUserIdSql(
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
            .Where2($"UserId=@{nameof(IdentityUser.Id)}")
            .AddTemplate(
                "SELECT /**select2**/FROM dbo.AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
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
            .Where2($"UserId=@{nameof(IdentityUser.Id)}")
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .AddTemplate(
                "SELECT /**select2**/FROM dbo.AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserLoginGetByLoginProviderKeySql(
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
            .Where2($"{nameof(IdentityUserLogin<int>.LoginProvider)}=@{nameof(IdentityUserLogin<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserLogin<int>.ProviderKey)}=@{nameof(IdentityUserLogin<int>.ProviderKey)}")
            .AddTemplate(
                "SELECT /**select2**/FROM dbo.AspNetUserLogins\r\n/**where2**/;")
            .RawSql;
    }
}
