using System.Collections.Generic;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Services;

public interface ITokenService
{
    Token GenerateToken(
        ApplicationUser user,
        IList<string> roles,
        IList<Claim> claims);

    RefreshToken? GetRefreshToken(string refreshTokenId);
}
