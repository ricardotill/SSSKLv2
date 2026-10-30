using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Orders.Queries.GetOrderInitialize;

internal sealed class GetOrderInitializeHandler(
    IProductRepository productRepository,
    IApplicationUserRepository applicationUserRepository)
    : IRequestHandler<GetOrderInitializeQuery, OrderInitializeDto>
{
    public async Task<OrderInitializeDto> Handle(GetOrderInitializeQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAvailable();
        var users = await applicationUserRepository.GetAll();

        return new OrderInitializeDto
        {
            Products = products.Select(p => new OrderInitializeProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Stock = p.Stock,
                Price = p.Price
            }).ToList(),
            Users = users.Select(u => new OrderInitializeUserDto
            {
                Id = u.Id,
                FullName = u.FullName
            }).ToList()
        };
    }
}
