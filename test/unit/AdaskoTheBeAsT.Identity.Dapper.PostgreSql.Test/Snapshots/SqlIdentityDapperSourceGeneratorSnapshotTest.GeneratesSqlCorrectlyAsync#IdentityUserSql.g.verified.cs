//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql : IIdentityUserConcurrencySql, IIdentityUserPagingSql
    {
        public string CreateSql { get; } =
            """
            INSERT INTO aspnetusers(
                "id"
               ,"username"
               ,"normalizedusername"
               ,"email"
               ,"normalizedemail"
               ,"emailconfirmed"
               ,"passwordhash"
               ,"securitystamp"
               ,"concurrencystamp"
               ,"phonenumber"
               ,"phonenumberconfirmed"
               ,"twofactorenabled"
               ,"lockoutend"
               ,"lockoutenabled"
               ,"accessfailedcount"
               ,"isactive")
            VALUES(
                @Id
               ,@UserName
               ,@NormalizedUserName
               ,@Email
               ,@NormalizedEmail
               ,@EmailConfirmed
               ,@PasswordHash
               ,@SecurityStamp
               ,@ConcurrencyStamp
               ,@PhoneNumber
               ,@PhoneNumberConfirmed
               ,@TwoFactorEnabled
               ,@LockoutEnd
               ,@LockoutEnabled
               ,@AccessFailedCount
               ,@Active)
            RETURNING "id" AS "Id";
            """;

        public string UpdateSql { get; } =
            """
            UPDATE aspnetusers
            SET "username"=@UserName
               ,"normalizedusername"=@NormalizedUserName
               ,"email"=@Email
               ,"normalizedemail"=@NormalizedEmail
               ,"emailconfirmed"=@EmailConfirmed
               ,"passwordhash"=@PasswordHash
               ,"securitystamp"=@SecurityStamp
               ,"concurrencystamp"=@ConcurrencyStamp
               ,"phonenumber"=@PhoneNumber
               ,"phonenumberconfirmed"=@PhoneNumberConfirmed
               ,"twofactorenabled"=@TwoFactorEnabled
               ,"lockoutend"=@LockoutEnd
               ,"lockoutenabled"=@LockoutEnabled
               ,"accessfailedcount"=@AccessFailedCount
               ,"isactive"=@Active
            WHERE "id"=@Id
              AND ("concurrencystamp"=@OriginalConcurrencyStamp OR ("concurrencystamp" IS NULL AND @OriginalConcurrencyStamp IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM aspnetusers
            WHERE "id"=@Id
              AND ("concurrencystamp"=@ConcurrencyStamp OR ("concurrencystamp" IS NULL AND @ConcurrencyStamp IS NULL));
            """;

        public string FindByIdSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"username" AS "UserName"
                  ,"normalizedusername" AS "NormalizedUserName"
                  ,"email" AS "Email"
                  ,"normalizedemail" AS "NormalizedEmail"
                  ,"emailconfirmed" AS "EmailConfirmed"
                  ,"passwordhash" AS "PasswordHash"
                  ,"securitystamp" AS "SecurityStamp"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"phonenumber" AS "PhoneNumber"
                  ,"phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,"twofactorenabled" AS "TwoFactorEnabled"
                  ,"lockoutend" AS "LockoutEnd"
                  ,"lockoutenabled" AS "LockoutEnabled"
                  ,"accessfailedcount" AS "AccessFailedCount"
                  ,"isactive" AS "Active"
            FROM aspnetusers
            WHERE "id"=@Id;
            """;

        public string FindByNameSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"username" AS "UserName"
                  ,"normalizedusername" AS "NormalizedUserName"
                  ,"email" AS "Email"
                  ,"normalizedemail" AS "NormalizedEmail"
                  ,"emailconfirmed" AS "EmailConfirmed"
                  ,"passwordhash" AS "PasswordHash"
                  ,"securitystamp" AS "SecurityStamp"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"phonenumber" AS "PhoneNumber"
                  ,"phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,"twofactorenabled" AS "TwoFactorEnabled"
                  ,"lockoutend" AS "LockoutEnd"
                  ,"lockoutenabled" AS "LockoutEnabled"
                  ,"accessfailedcount" AS "AccessFailedCount"
                  ,"isactive" AS "Active"
            FROM aspnetusers
            WHERE "normalizedusername"=@NormalizedUserName;
            """;

        public string FindByEmailSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"username" AS "UserName"
                  ,"normalizedusername" AS "NormalizedUserName"
                  ,"email" AS "Email"
                  ,"normalizedemail" AS "NormalizedEmail"
                  ,"emailconfirmed" AS "EmailConfirmed"
                  ,"passwordhash" AS "PasswordHash"
                  ,"securitystamp" AS "SecurityStamp"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"phonenumber" AS "PhoneNumber"
                  ,"phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,"twofactorenabled" AS "TwoFactorEnabled"
                  ,"lockoutend" AS "LockoutEnd"
                  ,"lockoutenabled" AS "LockoutEnabled"
                  ,"accessfailedcount" AS "AccessFailedCount"
                  ,"isactive" AS "Active"
            FROM aspnetusers
            WHERE "normalizedemail"=@NormalizedEmail;
            """;

        public string GetUsersForClaimSql { get; } =
            """
            SELECT u."id" AS "Id"
                  ,u."username" AS "UserName"
                  ,u."normalizedusername" AS "NormalizedUserName"
                  ,u."email" AS "Email"
                  ,u."normalizedemail" AS "NormalizedEmail"
                  ,u."emailconfirmed" AS "EmailConfirmed"
                  ,u."passwordhash" AS "PasswordHash"
                  ,u."securitystamp" AS "SecurityStamp"
                  ,u."concurrencystamp" AS "ConcurrencyStamp"
                  ,u."phonenumber" AS "PhoneNumber"
                  ,u."phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,u."twofactorenabled" AS "TwoFactorEnabled"
                  ,u."lockoutend" AS "LockoutEnd"
                  ,u."lockoutenabled" AS "LockoutEnabled"
                  ,u."accessfailedcount" AS "AccessFailedCount"
                  ,u."isactive" AS "Active"
            FROM aspnetusers u INNER JOIN
                 aspnetuserclaims c ON u."id"=c."userid"
            WHERE c."claimtype"=@ClaimType
              AND c."claimvalue"=@ClaimValue;
            """;

        public string GetUsersInRoleSql { get; } =
            """
            SELECT u."id" AS "Id"
                  ,u."username" AS "UserName"
                  ,u."normalizedusername" AS "NormalizedUserName"
                  ,u."email" AS "Email"
                  ,u."normalizedemail" AS "NormalizedEmail"
                  ,u."emailconfirmed" AS "EmailConfirmed"
                  ,u."passwordhash" AS "PasswordHash"
                  ,u."securitystamp" AS "SecurityStamp"
                  ,u."concurrencystamp" AS "ConcurrencyStamp"
                  ,u."phonenumber" AS "PhoneNumber"
                  ,u."phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,u."twofactorenabled" AS "TwoFactorEnabled"
                  ,u."lockoutend" AS "LockoutEnd"
                  ,u."lockoutenabled" AS "LockoutEnabled"
                  ,u."accessfailedcount" AS "AccessFailedCount"
                  ,u."isactive" AS "Active"
            FROM aspnetusers u INNER JOIN
                 aspnetuserroles ur ON u."id"=ur."userid" INNER JOIN
                 aspnetroles r ON ur."roleid"=r."id"
            WHERE r."normalizedname"=@NormalizedName;
            """;

        public string GetUsersSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"username" AS "UserName"
                  ,"normalizedusername" AS "NormalizedUserName"
                  ,"email" AS "Email"
                  ,"normalizedemail" AS "NormalizedEmail"
                  ,"emailconfirmed" AS "EmailConfirmed"
                  ,"passwordhash" AS "PasswordHash"
                  ,"securitystamp" AS "SecurityStamp"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"phonenumber" AS "PhoneNumber"
                  ,"phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,"twofactorenabled" AS "TwoFactorEnabled"
                  ,"lockoutend" AS "LockoutEnd"
                  ,"lockoutenabled" AS "LockoutEnabled"
                  ,"accessfailedcount" AS "AccessFailedCount"
                  ,"isactive" AS "Active"
            FROM aspnetusers;
            """;

        public string GetUsersPageSql { get; } =
            """
            SELECT "id" AS "Id"
                  ,"username" AS "UserName"
                  ,"normalizedusername" AS "NormalizedUserName"
                  ,"email" AS "Email"
                  ,"normalizedemail" AS "NormalizedEmail"
                  ,"emailconfirmed" AS "EmailConfirmed"
                  ,"passwordhash" AS "PasswordHash"
                  ,"securitystamp" AS "SecurityStamp"
                  ,"concurrencystamp" AS "ConcurrencyStamp"
                  ,"phonenumber" AS "PhoneNumber"
                  ,"phonenumberconfirmed" AS "PhoneNumberConfirmed"
                  ,"twofactorenabled" AS "TwoFactorEnabled"
                  ,"lockoutend" AS "LockoutEnd"
                  ,"lockoutenabled" AS "LockoutEnabled"
                  ,"accessfailedcount" AS "AccessFailedCount"
                  ,"isactive" AS "Active"
            FROM aspnetusers
            ORDER BY "id"
            LIMIT @PageSize OFFSET @Offset;
            """;
    }
}
