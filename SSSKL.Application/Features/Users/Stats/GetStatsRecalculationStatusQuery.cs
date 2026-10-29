using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Stats;

/// <summary>Status of one recalculation job, or of the most recent one when <paramref name="JobId"/> is null.</summary>
public sealed record GetStatsRecalculationStatusQuery(Guid? JobId = null) : IRequest<RecalculationJobDto?>;
