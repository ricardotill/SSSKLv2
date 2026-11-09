using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.TopUps.Queries.GetTopUpById;

internal sealed class GetTopUpByIdHandler(ITopUpRepository topUpRepository)
    : IRequestHandler<GetTopUpByIdQuery, TopUpDto>
{
    public async Task<TopUpDto> Handle(GetTopUpByIdQuery request, CancellationToken cancellationToken)
    {
        var topUp = await topUpRepository.GetById(request.Id);
        return topUp.ToDto();
    }
}
