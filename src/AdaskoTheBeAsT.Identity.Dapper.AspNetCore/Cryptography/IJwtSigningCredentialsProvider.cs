using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public interface IJwtSigningCredentialsProvider
{
    SigningCredentials GetSigningCredentials();
}
