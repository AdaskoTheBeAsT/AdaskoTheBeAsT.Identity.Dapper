using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public interface ITokenSecretHasher
{
    string GenerateOpaqueToken(int byteCount = 32);

    string GenerateSixDigitCode();

    HashedSecret HashToken(string rawToken);

    IReadOnlyList<HashedSecret> ComputeCandidateTokenHashes(string rawToken);

    bool VerifyToken(
        string rawToken,
        string expectedHashHex,
        string keyId);

    HashedSecret HashScopedCode(
        string scope,
        string code);

    bool VerifyScopedCode(
        string scope,
        string code,
        string expectedHashHex,
        string keyId);
}
