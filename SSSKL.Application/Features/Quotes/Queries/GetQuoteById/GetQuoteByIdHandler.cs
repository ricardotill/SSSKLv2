using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Quotes.Queries.GetQuoteById;

internal sealed class GetQuoteByIdHandler(
    IQuoteRepository quoteRepository,
    IReactionRepository reactionRepository,
    IUserRoleProvider roleProvider,
    QuoteAccess quoteAccess) : IRequestHandler<GetQuoteByIdQuery, QuoteDto>
{
    public async Task<QuoteDto> Handle(GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.UserId);

        var quote = await QuoteAccess.GetExisting(quoteRepository, request.Id);

        // Hide role-restricted quotes from users without one of the roles
        if (quote.VisibleToRoles.Any())
        {
            if (request.UserId == null) throw new NotFoundException("Quote not found");
            var userRoles = await roleProvider.GetRoles(request.UserId);
            if (!userRoles.Contains(Roles.Admin) && !quote.VisibleToRoles.Any(r => userRoles.Contains(r.Name!)))
            {
                throw new NotFoundException("Quote not found");
            }
        }

        var dto = quote.ToDto();

        var voteInfo = await quoteRepository.GetVoteSummaries([request.Id], request.UserId);
        if (voteInfo.TryGetValue(request.Id, out var vInfo))
        {
            dto.VoteCount = vInfo.Count;
            dto.HasVoted = vInfo.HasVoted;
        }
        dto.CommentsCount = await reactionRepository.CountByTarget(ReactionTargetType.Quote, request.Id);

        return dto;
    }
}
