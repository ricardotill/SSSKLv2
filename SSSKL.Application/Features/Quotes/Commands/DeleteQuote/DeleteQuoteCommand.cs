using MediatR;

namespace SSSKL.Application.Features.Quotes.Commands.DeleteQuote;

public sealed record DeleteQuoteCommand(Guid Id, string UserId) : IRequest;
