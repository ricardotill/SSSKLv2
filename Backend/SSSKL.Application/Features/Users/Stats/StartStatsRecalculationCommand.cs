using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Stats;

/// <summary>Starts recalculating every user's statistics in the background.</summary>
public sealed record StartStatsRecalculationCommand(string StartedByUserId) : IRequest<StartStatsRecalculationResult>;

/// <param name="Started">False when another recalculation was still running; <paramref name="Job"/> is then that job.</param>
public sealed record StartStatsRecalculationResult(RecalculationJobDto Job, bool Started);
