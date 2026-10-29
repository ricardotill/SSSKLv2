using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.TopUps.Queries.GetPersonalTopUps;

internal sealed class GetPersonalTopUpsHandler(ITopUpRepository topUpRepository)
    : IRequestHandler<GetPersonalTopUpsQuery, PaginationObject<TopUpDto>>
{
    public async Task<PaginationObject<TopUpDto>> Handle(GetPersonalTopUpsQuery request, CancellationToken cancellationToken)
    {
        var list = await topUpRepository.GetPersonal(request.Username, request.Take, request.Skip);
        var count = await topUpRepository.GetPersonalCount(request.Username);

        return new PaginationObject<TopUpDto>
        {
            Items = list.Select(t => t.ToDto()).ToList(),
            TotalCount = count
        };
    }
}
