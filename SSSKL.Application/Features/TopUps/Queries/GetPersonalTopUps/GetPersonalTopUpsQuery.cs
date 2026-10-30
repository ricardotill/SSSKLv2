using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.TopUps.Queries.GetPersonalTopUps;

public sealed record GetPersonalTopUpsQuery(string Username, int Skip, int Take) : IRequest<PaginationObject<TopUpDto>>;
