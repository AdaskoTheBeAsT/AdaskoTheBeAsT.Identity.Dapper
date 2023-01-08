using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityRoleClassGenerator
    : IdentityRoleClassGeneratorBase
{
    private readonly IIdentityHelper _identityHelper;

    public MySqlIdentityRoleClassGenerator()
    {
        _identityHelper = new MySqlIdentityHelper();
    }

    protected override string ProcessIdentityRoleCreateSql(
        string keyTypeName,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var template = _identityHelper.GetInsertTemplate("`aspnetroles`", keyTypeName);
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(template)
            .RawSql;
    }

    protected override string ProcessIdentityRoleUpdateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount);
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}`=@{propertyNames[i]}");
        }

        return sqlBuilder
            .Set2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                "UPDATE `aspnetroles`\r\n/**set2**//**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleDeleteSql() =>
        $"DELETE FROM `aspnetroles` WHERE {nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)};";

    protected override string ProcessIdentityRoleFindByIdSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { nameof(IdentityRole.Id) };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}` AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.Id)}=@{nameof(IdentityRole.Id)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetroles`\r\n/**where2**/;")
            .RawSql;
    }

    protected override string ProcessIdentityRoleFindByNameSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount) { nameof(IdentityRole.Id) };
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"`{columnNames[i]}` AS {propertyNames[i]}");
        }

        return sqlBuilder
            .Select2(string.Join("\r\n,", list))
            .Where2($"{nameof(IdentityRole.NormalizedName)}=@{nameof(IdentityRole.NormalizedName)}")
            .AddTemplate(
                "SELECT /**select2**/FROM `aspnetroles`\r\n/**where2**/;")
            .RawSql;
    }
}
