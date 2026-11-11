using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Quotes.Commands.UpdateQuote;

public sealed record UpdateQuoteCommand(Guid Id, QuoteUpdateDto Quote, string UserId) : IRequest<QuoteDto>;
