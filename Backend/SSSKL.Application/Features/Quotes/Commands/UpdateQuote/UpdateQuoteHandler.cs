using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Quotes.Commands.UpdateQuote;

internal sealed class UpdateQuoteHandler(
    IQuoteRepository quoteRepository,
    IRoleRepository roleRepository,
    QuoteAccess quoteAccess) : IRequestHandler<UpdateQuoteCommand, QuoteDto>
{
    public async Task<QuoteDto> Handle(UpdateQuoteCommand request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.UserId);

        var quote = await QuoteAccess.GetExisting(quoteRepository, request.Id);
        await quoteAccess.EnsureUserCanModify(quote, request.UserId);

        var dto = request.Quote;
        quote.Text = dto.Text;
        quote.DateSaid = dto.DateSaid;

        quote.VisibleToRoles.Clear();
        if (dto.VisibleToRoles != null && dto.VisibleToRoles.Any())
        {
            var newRoles = await roleRepository.GetByNames(dto.VisibleToRoles);
            foreach (var r in newRoles) quote.VisibleToRoles.Add(r);
        }

        QuoteAccess.ApplyAuthors(quote, dto.Authors);

        await quoteRepository.Update(quote);
        return quote.ToDto();
    }
}
