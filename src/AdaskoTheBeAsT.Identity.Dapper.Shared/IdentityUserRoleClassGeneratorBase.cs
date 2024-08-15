using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserRoleClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserRoleClassGenerator
{
    public string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserRoleSql", "IIdentityUserRoleSql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdRoleIdSql(sb, config, propertyColumnTypeTriples);
        GenerateGetCountSql(sb, config, propertyColumnTypeTriples);
        GenerateGetRoleNamesByUserIdSql(sb, config, propertyColumnTypeTriples);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityUserRole<>), insertOwnId, customs);

    protected abstract string ProcessIdentityUserRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserRoleCreateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdRoleIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserRoleGetByUserIdRoleIdSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetByUserIdRoleIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetCountSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserRoleGetCount(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetCountSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetRoleNamesByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserRoleGetRoleNamesByUserId(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetRoleNamesByUserIdSql {{ get; }} =
            @""{content}"";");
    }
}
