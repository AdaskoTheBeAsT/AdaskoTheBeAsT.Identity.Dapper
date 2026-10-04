using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserClassGenerator
{
    public string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        config = config.ForGeneration("IdentityUser", Provider, propertyColumnTypeTriples);
        var sb = new StringBuilder();
        GenerateSqlUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateSqlClassStart(sb, "IdentityUserSql", "IIdentityUserConcurrencySql, IIdentityUserPagingSql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateUpdateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateFindByIdSql(sb, config, propertyColumnTypeTriples);
        GenerateFindByNameSql(sb, config, propertyColumnTypeTriples);
        GenerateFindByEmailSql(sb, config, propertyColumnTypeTriples);
        GenerateGetUsersForClaimSql(sb, config, propertyColumnTypeTriples);
        GenerateGetUsersInRoleSql(sb, config, propertyColumnTypeTriples);
        GenerateGetUsersSql(sb, config, propertyColumnTypeTriples);
        sb.AppendLine();
        sb.AppendLine(
            $$"""
                    public string GetUsersPageSql { get; } =
                        {{RawStringLiteral.Format(Sql(config, propertyColumnTypeTriples).GetPage())}};
            """);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityUser<>), insertOwnId, customs);

    protected abstract string ProcessIdentityUserCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserUpdateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserFindByNameSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserFindByEmailSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserGetUsersForClaimSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserGetUsersInRoleSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserGetUsersSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserCreateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string CreateSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = AddConcurrencyPredicate(
            ProcessIdentityUserUpdateSql(config, propertyColumnTypeTriples),
            config,
            "OriginalConcurrencyStamp");
        sb.AppendLine(
            $$"""
                    public string UpdateSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = AddConcurrencyPredicate(
            ProcessIdentityUserDeleteSql(config), config, "ConcurrencyStamp");
        sb.AppendLine(
            $$"""
                    public string DeleteSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserFindByIdSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string FindByIdSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserFindByNameSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string FindByNameSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateFindByEmailSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserFindByEmailSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string FindByEmailSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetUsersForClaimSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserGetUsersForClaimSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetUsersForClaimSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetUsersInRoleSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserGetUsersInRoleSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetUsersInRoleSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetUsersSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserGetUsersSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetUsersSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
    }
}
