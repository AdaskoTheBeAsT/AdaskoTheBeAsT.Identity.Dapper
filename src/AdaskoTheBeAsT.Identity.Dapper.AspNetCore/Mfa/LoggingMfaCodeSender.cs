using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Mfa;

public sealed partial class LoggingMfaCodeSender<TUser> : IMfaCodeSender<TUser>
    where TUser : class
{
    private readonly ILogger<LoggingMfaCodeSender<TUser>> _logger;

    public LoggingMfaCodeSender(ILogger<LoggingMfaCodeSender<TUser>> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task SendEmailCodeAsync(
        TUser user,
        string email,
        string code,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        LogUnconfiguredDelivery(_logger, "Email");
        return Task.CompletedTask;
    }

    public Task SendSmsCodeAsync(
        TUser user,
        string phoneNumber,
        string code,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        LogUnconfiguredDelivery(_logger, "Sms");
        return Task.CompletedTask;
    }

    public Task SendSetupLinkAsync(
        TUser user,
        string destination,
        Uri setupUri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(setupUri);
        cancellationToken.ThrowIfCancellationRequested();

        LogUnconfiguredDelivery(_logger, "SetupLink");
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "No custom IMfaCodeSender is registered; MFA {Channel} message was not delivered externally.")]
    private static partial void LogUnconfiguredDelivery(
        ILogger logger,
        string channel);
}
