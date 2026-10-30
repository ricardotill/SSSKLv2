using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.TopUps.Queries.GetTopUps;

public sealed record GetTopUpsQuery(int Skip, int Take) : IRequest<PaginationObject<TopUpDto>>;
