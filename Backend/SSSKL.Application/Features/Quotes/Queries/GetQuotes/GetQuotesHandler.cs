using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Quotes.Queries.GetQuotes;

internal sealed class GetQuotesHandler(
    IQuoteRepository quoteRepository,
    IReactionRepository reactionRepository,
    IUserRoleProvider roleProvider,
    QuoteAccess quoteAccess) : IRequestHandler<GetQuotesQuery, PaginationObject<QuoteDto>>
{
    public async Task<PaginationObject<QuoteDto>> Handle(GetQuotesQuery request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.UserId);

        IList<string>? userRoles = null;
        var isAdmin = false;
        if (request.UserId != null)
        {
            userRoles = await roleProvider.GetRoles(request.UserId);
            isAdmin = userRoles.Contains(Roles.Admin);
        }

        var (quotes, totalCount) = await quoteRepository.GetAll(request.Skip, request.Take, userRoles, isAdmin, request.TargetUserId);
        var quoteDtos = quotes.Select(q => q.ToDto()).ToList();
        var quoteIds = quoteDtos.Select(q => q.Id).ToList();

        var voteInfo = await quoteRepository.GetVoteSummaries(quoteIds, request.UserId);
        // All reactions on quotes are comments
        var commentCounts = await reactionRepository.CountByTargets(ReactionTargetType.Quote, quoteIds);

        foreach (var dto in quoteDtos)
        {
            if (voteInfo.TryGetValue(dto.Id, out var vInfo))
            {
                dto.VoteCount = vInfo.Count;
                dto.HasVoted = vInfo.HasVoted;
            }

            if (commentCounts.TryGetValue(dto.Id, out var cCount))
            {
                dto.CommentsCount = cCount;
            }
        }

        return new PaginationObject<QuoteDto>
        {
            Items = quoteDtos,
            TotalCount = totalCount
        };
    }
}
