using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Quotes.Queries.GetQuoteById;

/// <summary>Throws NotFoundException when the quote doesn't exist or isn't visible to the user.</summary>
public sealed record GetQuoteByIdQuery(Guid Id, string? UserId) : IRequest<QuoteDto>;
