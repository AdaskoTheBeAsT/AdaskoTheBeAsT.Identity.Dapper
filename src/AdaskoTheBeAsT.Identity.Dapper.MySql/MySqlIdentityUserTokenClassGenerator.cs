using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserTokenClassGenerator
    : IdentityUserTokenClassGeneratorBase
{
    protected override string ProcessIdentityUserTokenCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnPairs.Select(s => $"`{s.ColumnName}`")))
            .Values(string.Join("\r\n,", propertyColumnPairs.Select(s => $"@{s.PropertyName}")))
            .AddTemplate(
                $"INSERT INTO {config.SchemaPart}`aspnetusertokens`(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);")
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
                $"DELETE FROM {config.SchemaPart}`aspnetusertokens`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityUserTokenGetByUserIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnPairs.Count);
        foreach (var localPair in propertyColumnPairs)
        {
            list.Add($"`{localPair.ColumnName}` AS {localPair.PropertyName}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityUserToken<int>.UserId)}=@{nameof(IdentityUserToken<int>.UserId)}")
            .Where2($"{nameof(IdentityUserToken<int>.LoginProvider)}=@{nameof(IdentityUserToken<int>.LoginProvider)}")
            .Where2($"{nameof(IdentityUserToken<int>.Name)}=@{nameof(IdentityUserToken<int>.Name)}")
            .AddTemplate(
                $"SELECT TOP 1 /**select2**/FROM {config.SchemaPart}`aspnetusertokens`\r\n/**where2**/;")
            .RawSql;
    }
}
