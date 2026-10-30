using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Products.Queries.GetAvailableProducts;

internal sealed class GetAvailableProductsHandler(IProductRepository productRepository)
    : IRequestHandler<GetAvailableProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetAvailableProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAvailable();
        return products.Select(p => p.ToDto()).ToList();
    }
}
