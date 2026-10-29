using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Reactions.Queries.GetReactionTimeline;

public sealed record GetReactionTimelineQuery(int Skip, int Take) : IRequest<IReadOnlyList<ReactionDto>>;
