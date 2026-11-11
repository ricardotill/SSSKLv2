using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int Skip, int Take) : IRequest<PaginationObject<ProductDto>>;
