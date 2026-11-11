using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Quotes.Queries.GetQuotes;

public sealed record GetQuotesQuery(int Skip, int Take, string? UserId, string? TargetUserId) : IRequest<PaginationObject<QuoteDto>>;
