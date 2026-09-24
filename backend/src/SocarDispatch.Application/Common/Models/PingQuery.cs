using MediatR;
using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.Application.Common.Models;

public record PingQuery : IRequest<ApiResponse<string>>;

public class PingQueryHandler : IRequestHandler<PingQuery, ApiResponse<string>>
{
    public Task<ApiResponse<string>> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ApiResponse<string>.SuccessResult("SOCAR Dispatch API & CQRS Pipeline is healthy!", "Success"));
    }
}
