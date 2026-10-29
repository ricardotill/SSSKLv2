using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.TopUps.Queries.GetTopUpById;

public sealed record GetTopUpByIdQuery(Guid Id) : IRequest<TopUpDto>;
