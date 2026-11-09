using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, ProductUpdateDto Product) : IRequest;
