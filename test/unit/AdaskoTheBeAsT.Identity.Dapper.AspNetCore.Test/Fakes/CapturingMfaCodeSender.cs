using System.Collections.Concurrent;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed class CapturingMfaCodeSender : IMfaCodeSender<TestUser>
{
    private readonly ConcurrentQueue<SentMfaCode> _sent = new();

    public IReadOnlyCollection<SentMfaCode> SentCodes => _sent.ToArray();

    public SentMfaCode? LastSent { get; private set; }

    public Uri? LastSetupUri { get; private set; }

    public Task SendEmailCodeAsync(
        TestUser user,
        string email,
        string code,
        CancellationToken cancellationToken)
    {
        var item = new SentMfaCode(email, code, MfaMethodType.Email);
        LastSent = item;
        _sent.Enqueue(item);
        return Task.CompletedTask;
    }

    public Task SendSmsCodeAsync(
        TestUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken)
    {
        var item = new SentMfaCode(phoneNumber, code, MfaMethodType.Sms);
        LastSent = item;
        _sent.Enqueue(item);
        return Task.CompletedTask;
    }

    public Task SendSetupLinkAsync(
        TestUser user,
        string destination,
        Uri setupUri,
        CancellationToken cancellationToken)
    {
        LastSetupUri = setupUri;
        return Task.CompletedTask;
    }
}
