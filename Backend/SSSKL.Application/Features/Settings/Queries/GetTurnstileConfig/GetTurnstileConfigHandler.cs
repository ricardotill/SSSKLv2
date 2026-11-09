using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Services;

namespace SSSKL.Application.Features.Settings.Queries.GetTurnstileConfig;

internal sealed class GetTurnstileConfigHandler(ITurnstileService turnstile)
    : IRequestHandler<GetTurnstileConfigQuery, TurnstileConfigDto>
{
    public async Task<TurnstileConfigDto> Handle(GetTurnstileConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await turnstile.GetClientConfigAsync(cancellationToken);
        return new TurnstileConfigDto { Enabled = config.Enabled, SiteKey = config.SiteKey };
    }
}
