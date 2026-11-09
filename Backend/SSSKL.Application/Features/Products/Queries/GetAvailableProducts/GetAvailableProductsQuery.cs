using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Products.Queries.GetAvailableProducts;

public sealed record GetAvailableProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
