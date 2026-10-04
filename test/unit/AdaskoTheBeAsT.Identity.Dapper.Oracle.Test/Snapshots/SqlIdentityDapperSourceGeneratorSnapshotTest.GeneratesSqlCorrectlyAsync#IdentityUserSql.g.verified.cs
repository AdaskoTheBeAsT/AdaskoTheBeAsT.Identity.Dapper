//HintName: IdentityUserSql.g.cs
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class IdentityUserSql : IIdentityUserConcurrencySql, IIdentityUserPagingSql
    {
        public string CreateSql { get; } =
            """
            BEGIN
            INSERT INTO AspNetUsers(
                "ID"
               ,"USERNAME"
               ,"NORMALIZEDUSERNAME"
               ,"EMAIL"
               ,"NORMALIZEDEMAIL"
               ,"EMAILCONFIRMED"
               ,"PASSWORDHASH"
               ,"SECURITYSTAMP"
               ,"CONCURRENCYSTAMP"
               ,"PHONENUMBER"
               ,"PHONENUMBERCONFIRMED"
               ,"TWOFACTORENABLED"
               ,"LOCKOUTEND"
               ,"LOCKOUTENABLED"
               ,"ACCESSFAILEDCOUNT"
               ,"ISACTIVE")
            VALUES(
                :Id
               ,:UserName
               ,:NormalizedUserName
               ,:Email
               ,:NormalizedEmail
               ,:EmailConfirmed
               ,:PasswordHash
               ,:SecurityStamp
               ,:ConcurrencyStamp
               ,:PhoneNumber
               ,:PhoneNumberConfirmed
               ,:TwoFactorEnabled
               ,:LockoutEnd
               ,:LockoutEnabled
               ,:AccessFailedCount
               ,:Active)
            RETURNING "ID" INTO :OutputId;
            END;
            """;

        public string UpdateSql { get; } =
            """
            UPDATE AspNetUsers
            SET "USERNAME"=:UserName
               ,"NORMALIZEDUSERNAME"=:NormalizedUserName
               ,"EMAIL"=:Email
               ,"NORMALIZEDEMAIL"=:NormalizedEmail
               ,"EMAILCONFIRMED"=:EmailConfirmed
               ,"PASSWORDHASH"=:PasswordHash
               ,"SECURITYSTAMP"=:SecurityStamp
               ,"CONCURRENCYSTAMP"=:ConcurrencyStamp
               ,"PHONENUMBER"=:PhoneNumber
               ,"PHONENUMBERCONFIRMED"=:PhoneNumberConfirmed
               ,"TWOFACTORENABLED"=:TwoFactorEnabled
               ,"LOCKOUTEND"=:LockoutEnd
               ,"LOCKOUTENABLED"=:LockoutEnabled
               ,"ACCESSFAILEDCOUNT"=:AccessFailedCount
               ,"ISACTIVE"=:Active
            WHERE "ID"=:Id
              AND ("CONCURRENCYSTAMP"=:OriginalConcurrencyStamp OR ("CONCURRENCYSTAMP" IS NULL AND :OriginalConcurrencyStamp IS NULL));
            """;

        public string DeleteSql { get; } =
            """
            DELETE FROM AspNetUsers
            WHERE "ID"=:Id
              AND ("CONCURRENCYSTAMP"=:ConcurrencyStamp OR ("CONCURRENCYSTAMP" IS NULL AND :ConcurrencyStamp IS NULL));
            """;

        public string FindByIdSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"USERNAME" AS "USERNAME"
                  ,"NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,"EMAIL" AS "EMAIL"
                  ,"NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,"EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,"PASSWORDHASH" AS "PASSWORDHASH"
                  ,"SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"PHONENUMBER" AS "PHONENUMBER"
                  ,"PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,"TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,"LOCKOUTEND" AS "LOCKOUTEND"
                  ,"LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,"ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers
            WHERE "ID"=:Id;
            """;

        public string FindByNameSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"USERNAME" AS "USERNAME"
                  ,"NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,"EMAIL" AS "EMAIL"
                  ,"NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,"EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,"PASSWORDHASH" AS "PASSWORDHASH"
                  ,"SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"PHONENUMBER" AS "PHONENUMBER"
                  ,"PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,"TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,"LOCKOUTEND" AS "LOCKOUTEND"
                  ,"LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,"ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers
            WHERE "NORMALIZEDUSERNAME"=:NormalizedUserName;
            """;

        public string FindByEmailSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"USERNAME" AS "USERNAME"
                  ,"NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,"EMAIL" AS "EMAIL"
                  ,"NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,"EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,"PASSWORDHASH" AS "PASSWORDHASH"
                  ,"SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"PHONENUMBER" AS "PHONENUMBER"
                  ,"PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,"TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,"LOCKOUTEND" AS "LOCKOUTEND"
                  ,"LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,"ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers
            WHERE "NORMALIZEDEMAIL"=:NormalizedEmail;
            """;

        public string GetUsersForClaimSql { get; } =
            """
            SELECT u."ID" AS "ID"
                  ,u."USERNAME" AS "USERNAME"
                  ,u."NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,u."EMAIL" AS "EMAIL"
                  ,u."NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,u."EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,u."PASSWORDHASH" AS "PASSWORDHASH"
                  ,u."SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,u."CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,u."PHONENUMBER" AS "PHONENUMBER"
                  ,u."PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,u."TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,u."LOCKOUTEND" AS "LOCKOUTEND"
                  ,u."LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,u."ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,u."ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers u INNER JOIN
                 AspNetUserClaims c ON u."ID"=c."USERID"
            WHERE c."CLAIMTYPE"=:ClaimType
              AND c."CLAIMVALUE"=:ClaimValue;
            """;

        public string GetUsersInRoleSql { get; } =
            """
            SELECT u."ID" AS "ID"
                  ,u."USERNAME" AS "USERNAME"
                  ,u."NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,u."EMAIL" AS "EMAIL"
                  ,u."NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,u."EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,u."PASSWORDHASH" AS "PASSWORDHASH"
                  ,u."SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,u."CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,u."PHONENUMBER" AS "PHONENUMBER"
                  ,u."PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,u."TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,u."LOCKOUTEND" AS "LOCKOUTEND"
                  ,u."LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,u."ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,u."ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers u INNER JOIN
                 AspNetUserRoles ur ON u."ID"=ur."USERID" INNER JOIN
                 AspNetRoles r ON ur."ROLEID"=r."ID"
            WHERE r."NORMALIZEDNAME"=:NormalizedName;
            """;

        public string GetUsersSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"USERNAME" AS "USERNAME"
                  ,"NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,"EMAIL" AS "EMAIL"
                  ,"NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,"EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,"PASSWORDHASH" AS "PASSWORDHASH"
                  ,"SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"PHONENUMBER" AS "PHONENUMBER"
                  ,"PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,"TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,"LOCKOUTEND" AS "LOCKOUTEND"
                  ,"LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,"ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers;
            """;

        public string GetUsersPageSql { get; } =
            """
            SELECT "ID" AS "ID"
                  ,"USERNAME" AS "USERNAME"
                  ,"NORMALIZEDUSERNAME" AS "NORMALIZEDUSERNAME"
                  ,"EMAIL" AS "EMAIL"
                  ,"NORMALIZEDEMAIL" AS "NORMALIZEDEMAIL"
                  ,"EMAILCONFIRMED" AS "EMAILCONFIRMED"
                  ,"PASSWORDHASH" AS "PASSWORDHASH"
                  ,"SECURITYSTAMP" AS "SECURITYSTAMP"
                  ,"CONCURRENCYSTAMP" AS "CONCURRENCYSTAMP"
                  ,"PHONENUMBER" AS "PHONENUMBER"
                  ,"PHONENUMBERCONFIRMED" AS "PHONENUMBERCONFIRMED"
                  ,"TWOFACTORENABLED" AS "TWOFACTORENABLED"
                  ,"LOCKOUTEND" AS "LOCKOUTEND"
                  ,"LOCKOUTENABLED" AS "LOCKOUTENABLED"
                  ,"ACCESSFAILEDCOUNT" AS "ACCESSFAILEDCOUNT"
                  ,"ISACTIVE" AS "ACTIVE"
            FROM AspNetUsers
            ORDER BY "ID"
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;
    }
}
