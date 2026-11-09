using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.TopUps.Queries.GetTopUps;

internal sealed class GetTopUpsHandler(ITopUpRepository topUpRepository)
    : IRequestHandler<GetTopUpsQuery, PaginationObject<TopUpDto>>
{
    public async Task<PaginationObject<TopUpDto>> Handle(GetTopUpsQuery request, CancellationToken cancellationToken)
    {
        var list = await topUpRepository.GetAll(request.Take, request.Skip);
        var count = await topUpRepository.GetCount();

        return new PaginationObject<TopUpDto>
        {
            Items = list.Select(t => t.ToDto()).ToList(),
            TotalCount = count
        };
    }
}
