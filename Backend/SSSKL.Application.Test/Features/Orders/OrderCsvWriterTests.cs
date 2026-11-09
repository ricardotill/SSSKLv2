using System.Globalization;
using FluentAssertions;
using SSSKL.Application.Features.Orders;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Features.Orders;

[TestClass]
public class OrderCsvWriterTests
{
    private const string Header = "OrderId,CustomerUsername,OrderDateTime,ProductName,ProductAmount,TotalPaid";

    [TestMethod]
    public void Write_WithOrders_ShouldReturnValidCsv()
    {
        // Arrange
        var orders = new List<Order>
        {
            OrderTestData.CreateOrder(Guid.NewGuid(), "user1", "Product 1", 2, 10.00m, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
            OrderTestData.CreateOrder(Guid.NewGuid(), "user2", "Product 2", 3, 15.00m, new DateTime(2025, 9, 2, 0, 0, 0, DateTimeKind.Utc))
        };

        // Act
        var result = OrderCsvWriter.Write(orders);

        // Assert
        result.Should().StartWith(Header);
        foreach (var order in orders)
        {
            result.Should().Contain($"{order.Id},\"{order.User.UserName}\",{order.CreatedOn:yyyy-MM-dd HH:mm:ss},\"{order.ProductNaam}\",{order.Amount},{order.Paid.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    [TestMethod]
    public void Write_WithNoOrders_ShouldReturnHeaderOnly()
    {
        OrderCsvWriter.Write([]).Should().Be(Header + Environment.NewLine);
    }

    [TestMethod]
    public void Write_EscapesQuotesAndFormulaPrefixes()
    {
        // Arrange
        var order = OrderTestData.CreateOrder(Guid.NewGuid(), "=cmd", "Say \"hi\"", 1, 1m);

        // Act
        var result = OrderCsvWriter.Write([order]);

        // Assert
        result.Should().Contain("\"'=cmd\"");
        result.Should().Contain("\"Say \"\"hi\"\"\"");
    }
}
