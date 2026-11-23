namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Tokens;

public interface IJwtTokenGenerator
{
    IssuedAccessToken GenerateAccessToken(JwtTokenDescriptorContext context);
}
