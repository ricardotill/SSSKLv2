using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Settings.Queries.GetTurnstileConfig;

/// <summary>What the login and sign-up pages need to render Turnstile.</summary>
public sealed record GetTurnstileConfigQuery : IRequest<TurnstileConfigDto>;
