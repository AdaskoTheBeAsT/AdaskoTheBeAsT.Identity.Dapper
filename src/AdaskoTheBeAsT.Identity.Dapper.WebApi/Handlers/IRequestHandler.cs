using System.Threading;
using System.Threading.Tasks;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;

public interface IRequestHandler<in TRequest, TResponse>
{
#pragma warning disable VSTHRD200 // Preserve compatibility with existing handler method signature.
    Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken);
#pragma warning restore VSTHRD200
}
