using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Features.Quotes.Commands.CreateQuote;

internal sealed class CreateQuoteHandler(
    IQuoteRepository quoteRepository,
    IRoleRepository roleRepository,
    IUserNotifier userNotifier,
    IDomainEventDispatcher eventDispatcher,
    QuoteAccess quoteAccess) : IRequestHandler<CreateQuoteCommand, QuoteDto>
{
    public async Task<QuoteDto> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.CreatorId);

        var dto = request.Quote;
        var quote = new Quote
        {
            Text = dto.Text,
            DateSaid = dto.DateSaid,
            CreatedById = request.CreatorId,
            CreatedOn = DateTime.UtcNow
        };

        if (dto.VisibleToRoles != null && dto.VisibleToRoles.Any())
        {
            quote.VisibleToRoles = await roleRepository.GetByNames(dto.VisibleToRoles);
        }

        QuoteAccess.ApplyAuthors(quote, dto.Authors);

        await quoteRepository.Add(quote);
        await eventDispatcher.DispatchAsync(new QuoteCreatedEvent(quote));

        // Fetch again to include navigations for the mapping
        var createdQuote = await QuoteAccess.GetExisting(quoteRepository, quote.Id);

        if (dto.SendNotification)
        {
            await SendNewQuoteNotification(createdQuote, dto.VisibleToRoles);
        }

        return createdQuote.ToDto();
    }

    private async Task SendNewQuoteNotification(Quote quote, IEnumerable<string>? visibleToRoles)
    {
        var authorNames = quote.Authors.Select(a => a.CustomName ?? a.ApplicationUser?.FullName ?? "Iemand").ToList();
        var authorsText = authorNames.Count > 0 ? string.Join(" & ", authorNames) : "Iemand";

        // Restricted quotes only notify users with one of the roles; otherwise everyone
        IList<string>? targetUserIds = null;
        if (visibleToRoles != null && visibleToRoles.Any())
        {
            targetUserIds = await roleRepository.GetUserIdsInRoles(visibleToRoles);
        }

        await userNotifier.NotifyManyAsync(
            targetUserIds,
            "Nieuwe Quote! 💬",
            $"{authorsText}: \"{quote.Text}\"",
            "/quotes",
            sendPush: true);
    }
}
