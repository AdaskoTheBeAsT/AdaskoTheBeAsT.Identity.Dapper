namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public readonly record struct HashedSecret(
    string HashHex,
    string KeyId);
