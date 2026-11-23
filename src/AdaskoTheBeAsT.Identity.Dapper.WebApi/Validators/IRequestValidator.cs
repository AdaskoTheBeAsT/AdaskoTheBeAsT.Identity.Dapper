namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Validators;

public interface IRequestValidator<in TRequest>
{
    void ValidateAndThrow(TRequest request);
}
