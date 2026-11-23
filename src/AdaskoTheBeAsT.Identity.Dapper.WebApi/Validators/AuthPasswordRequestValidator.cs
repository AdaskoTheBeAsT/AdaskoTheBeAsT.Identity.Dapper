using System;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;

public sealed class AuthPasswordRequestValidator : IRequestValidator<AuthPasswordRequest>
{
    public void ValidateAndThrow(AuthPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Username) ||
            request.Username.Length > 256 ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length > 256)
        {
            throw new RequestValidationException("Username and password must be between 1 and 256 characters.");
        }
    }
}
