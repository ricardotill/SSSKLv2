using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Products.Queries.GetProducts;

internal sealed class GetProductsHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, PaginationObject<ProductDto>>
{
    public async Task<PaginationObject<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAll(request.Skip, request.Take);
        var totalCount = await productRepository.GetCount();

        return new PaginationObject<ProductDto>
        {
            Items = products.Select(p => p.ToDto()).ToList(),
            TotalCount = totalCount
        };
    }
}
