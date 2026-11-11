using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Products;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Stock = p.Stock,
        EnableLeaderboard = p.EnableLeaderboard
    };
}
