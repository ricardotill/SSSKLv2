using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Orders;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        CreatedOn = order.CreatedOn,
        ProductId = order.Product?.Id,
        ProductName = order.ProductNaam ?? string.Empty,
        UserId = Guid.TryParse(order.User?.Id, out var uid) ? uid : null,
        UserFullName = order.User?.FullName ?? string.Empty,
        Amount = order.Amount,
        Paid = order.Paid,
        ProfilePictureUrl = order.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{order.User.ProfileImageId}" : null
    };
}
