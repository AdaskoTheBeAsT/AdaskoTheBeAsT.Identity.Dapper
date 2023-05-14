using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using FluentValidation;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;

public class AuthPasswordRequestValidator
    : AbstractValidator<AuthPasswordRequest>
{
    public AuthPasswordRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty();
        RuleFor(r => r.Username).Length(1, 256);
        RuleFor(r => r.Password).NotEmpty();
        RuleFor(r => r.Password).Length(1, 256);
    }
}
