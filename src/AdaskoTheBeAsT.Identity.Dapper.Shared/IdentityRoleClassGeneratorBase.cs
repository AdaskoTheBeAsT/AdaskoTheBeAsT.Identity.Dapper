using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityRoleClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityRoleClassGenerator
{
    public string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityRoleSql", "IIdentityRoleSql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateUpdateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateFindByIdSql(sb, config, propertyColumnTypeTriples);
        GenerateFindByNameSql(sb, config, propertyColumnTypeTriples);
        GenerateGetRolesSql(sb, config, propertyColumnTypeTriples);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityRole<>), insertOwnId, customs);

    protected abstract string ProcessIdentityRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityRoleUpdateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityRoleFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityRoleFindByNameSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityRoleGetRolesSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleCreateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleUpdateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityRoleDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleFindByIdSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string FindByIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleFindByNameSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string FindByNameSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetRolesSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleGetRolesSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetRolesSql {{ get; }} =
            @""{content}"";");
    }
}
