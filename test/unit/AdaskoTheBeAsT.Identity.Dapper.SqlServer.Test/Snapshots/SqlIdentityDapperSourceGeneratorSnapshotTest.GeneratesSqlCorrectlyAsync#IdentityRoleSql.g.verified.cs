//HintName: IdentityRoleSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityRoleSql : IIdentityRoleConcurrencySql, IIdentityRolePagingSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO AspNetRoles(
                [Name]
               ,[NormalizedName]
               ,[ConcurrencyStamp]
               ,[IsActive])
            OUTPUT inserted.[Id]
            VALUES(
                @Name
               ,@NormalizedName
               ,@ConcurrencyStamp
               ,@Active);
            """;

        public string UpdateSql { get; } =
            """
            UPDATE AspNetRoles
            SET [Name]=@Name
               ,[NormalizedName]=@NormalizedName
               ,[ConcurrencyStamp]=@ConcurrencyStamp
               ,[IsActive]=@Active
            WHERE [Id]=@Id
              AND ([ConcurrencyStamp]=@OriginalConcurrencyStamp OR ([ConcurrencyStamp] IS NULL AND @OriginalConcurrencyStamp IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetRoles
            WHERE [Id]=@Id
              AND ([ConcurrencyStamp]=@ConcurrencyStamp OR ([ConcurrencyStamp] IS NULL AND @ConcurrencyStamp IS NULL));
            """;

        public string FindByIdSql { get; } =
            """
            SELECT [Id] AS [Id]
                  ,[Name] AS [Name]
                  ,[NormalizedName] AS [NormalizedName]
                  ,[ConcurrencyStamp] AS [ConcurrencyStamp]
                  ,[IsActive] AS [Active]
            FROM AspNetRoles
            WHERE [Id]=@Id;
            """;

        public string FindByNameSql { get; } =
            """
            SELECT [Id] AS [Id]
                  ,[Name] AS [Name]
                  ,[NormalizedName] AS [NormalizedName]
                  ,[ConcurrencyStamp] AS [ConcurrencyStamp]
                  ,[IsActive] AS [Active]
            FROM AspNetRoles
            WHERE [NormalizedName]=@NormalizedName;
            """;

        public string GetRolesSql { get; } =
            """
            SELECT [Id] AS [Id]
                  ,[Name] AS [Name]
                  ,[NormalizedName] AS [NormalizedName]
                  ,[ConcurrencyStamp] AS [ConcurrencyStamp]
                  ,[IsActive] AS [Active]
            FROM AspNetRoles;
            """;

        public string GetRolesPageSql { get; } =
            """
            SELECT [Id] AS [Id]
                  ,[Name] AS [Name]
                  ,[NormalizedName] AS [NormalizedName]
                  ,[ConcurrencyStamp] AS [ConcurrencyStamp]
                  ,[IsActive] AS [Active]
            FROM AspNetRoles
            ORDER BY [Id]
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
    }
}
