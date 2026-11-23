using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public interface IMfaCodeSender<in TUser>
    where TUser : class
{
    Task SendEmailCodeAsync(
        TUser user,
        string email,
        string code,
        CancellationToken cancellationToken);

    Task SendSmsCodeAsync(
        TUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken);

    Task SendSetupLinkAsync(
        TUser user,
        string destination,
        Uri setupUri,
        CancellationToken cancellationToken);
}
