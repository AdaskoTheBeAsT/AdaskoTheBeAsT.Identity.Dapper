using AdaskoTheBeAsT.Identity.Dapper.Attributes;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity;

[InsertOwnId]
public class ApplicationUser
    : AuditedUser
{
    public override string? NormalizedUserName
    {
        get
        {
            return UserName;
        }

#pragma warning disable S3237
        set
        {
            // noop
        }
#pragma warning restore S3237
    }

    public override string? NormalizedEmail
    {
        get
        {
            return Email;
        }

#pragma warning disable S3237
        set
        {
            // noop
        }
#pragma warning restore S3237
    }
}
