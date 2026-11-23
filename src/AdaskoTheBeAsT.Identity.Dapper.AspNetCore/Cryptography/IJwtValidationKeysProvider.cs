using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public interface IJwtValidationKeysProvider
{
    IReadOnlyList<SecurityKey> GetValidationKeys();

    IReadOnlyList<string> GetValidAlgorithms();
}
