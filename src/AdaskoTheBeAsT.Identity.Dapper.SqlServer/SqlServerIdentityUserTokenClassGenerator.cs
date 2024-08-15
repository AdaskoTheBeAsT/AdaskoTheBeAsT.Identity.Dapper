using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityUserTokenClassGenerator
    : IdentityUserTokenClassGeneratorBase
{
    protected override string ProcessIdentityUserTokenCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"[{s.ColumnName}]")))
            .Values(string.Join("\r\n,", propertyColumnTypeTriples.Select(s => $"@{s.PropertyName}")))
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var list = new List<string>(propertyColumnTypeTriples.Count);
        foreach (var localPair in propertyColumnTypeTriples)
        {
            list.Add($"[{localPair.ColumnName}] AS {localPair.PropertyName}");
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
