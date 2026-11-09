using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(ProductCreateDto Product) : IRequest<ProductDto>;
