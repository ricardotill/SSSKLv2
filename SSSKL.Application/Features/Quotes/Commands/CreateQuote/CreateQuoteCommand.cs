using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Quotes.Commands.CreateQuote;

public sealed record CreateQuoteCommand(QuoteCreateDto Quote, string CreatorId) : IRequest<QuoteDto>;
