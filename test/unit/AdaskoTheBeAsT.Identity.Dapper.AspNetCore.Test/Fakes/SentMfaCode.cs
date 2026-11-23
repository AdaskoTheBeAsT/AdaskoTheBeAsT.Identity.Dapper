using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Test.Fakes;

internal sealed record SentMfaCode(
    string Destination,
    string Code,
    MfaMethodType Channel);
