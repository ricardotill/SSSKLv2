using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Features.Orders;

internal static class OrderTestData
{
    public static Order CreateOrder(
        Guid id,
        string username,
        string productName,
        int amount,
        decimal paid,
        DateTime? createdOn = null)
    {
        return new Order
        {
            Id = id,
            User = new ApplicationUser { Id = $"{username}-id", UserName = username },
            ProductNaam = productName,
            Amount = amount,
            Paid = paid,
            CreatedOn = createdOn ?? DateTime.UtcNow
        };
    }

    public static Product CreateProduct(Guid id, string name, decimal price)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Price = price,
            Stock = 100
        };
    }

    public static ApplicationUser CreateUser(string username)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = $"{username}@example.com",
            Name = $"Test {username}",
            Surname = "User"
        };
    }
}
