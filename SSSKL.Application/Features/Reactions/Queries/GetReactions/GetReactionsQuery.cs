using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Reactions.Queries.GetReactions;

/// <summary>Top-level reactions on a target, each with its replies (two levels deep) flattened under it.</summary>
public sealed record GetReactionsQuery(Guid TargetId, string TargetType) : IRequest<IReadOnlyList<ReactionDto>>;
