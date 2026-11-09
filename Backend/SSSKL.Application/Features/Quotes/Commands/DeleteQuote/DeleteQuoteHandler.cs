using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Quotes.Commands.DeleteQuote;

internal sealed class DeleteQuoteHandler(
    IQuoteRepository quoteRepository,
    IUserStatRepository userStatRepository,
    QuoteAccess quoteAccess) : IRequestHandler<DeleteQuoteCommand>
{
    public async Task Handle(DeleteQuoteCommand request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.UserId);

        var quote = await QuoteAccess.GetExisting(quoteRepository, request.Id);
        await quoteAccess.EnsureUserCanModify(quote, request.UserId);

        var affectedUserIds = await quoteRepository.GetAuthorUserIds(request.Id);
        if (!affectedUserIds.Contains(quote.CreatedById))
        {
            affectedUserIds.Add(quote.CreatedById);
        }

        await quoteRepository.Delete(request.Id);
        foreach (var affectedUserId in affectedUserIds)
        {
            await userStatRepository.RecalculateByUserId(affectedUserId);
        }
    }
}
