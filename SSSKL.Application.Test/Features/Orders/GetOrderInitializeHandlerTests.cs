using FluentAssertions;
using NSubstitute;
using SSSKL.Application.Features.Orders.Queries.GetOrderInitialize;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Features.Orders;

[TestClass]
public class GetOrderInitializeHandlerTests
{
    [TestMethod]
    public async Task Handle_ReturnsAvailableProductsAndAllUsers()
    {
        // Arrange
        var products = Substitute.For<IProductRepository>();
        var users = Substitute.For<IApplicationUserRepository>();
        var product = new Product { Id = Guid.NewGuid(), Name = "P1", Stock = 3, Price = 1.5m };
        var user = new ApplicationUser { Id = "u1", UserName = "un1", Name = "Jan", Surname = "Jansen" };
        products.GetAllAvailable().Returns(new List<Product> { product });
        users.GetAll().Returns(new List<ApplicationUser> { user });
        var sut = new GetOrderInitializeHandler(products, users);

        // Act
        var result = await sut.Handle(new GetOrderInitializeQuery(), CancellationToken.None);

        // Assert
        result.Products.Should().ContainSingle().Which.Should().BeEquivalentTo(new { product.Id, product.Name, product.Stock, product.Price });
        result.Users.Should().ContainSingle().Which.Should().BeEquivalentTo(new { user.Id, user.FullName, ProfilePictureUrl = (string?)null });
    }

    [TestMethod]
    public async Task Handle_UserWithProfileImage_ReturnsProfilePictureUrl()
    {
        // Arrange
        var products = Substitute.For<IProductRepository>();
        var users = Substitute.For<IApplicationUserRepository>();
        var imageId = Guid.NewGuid();
        var user = new ApplicationUser { Id = "u1", UserName = "un1", Name = "Jan", Surname = "Jansen", ProfileImageId = imageId };
        products.GetAllAvailable().Returns(new List<Product>());
        users.GetAll().Returns(new List<ApplicationUser> { user });
        var sut = new GetOrderInitializeHandler(products, users);

        // Act
        var result = await sut.Handle(new GetOrderInitializeQuery(), CancellationToken.None);

        // Assert
        result.Users.Should().ContainSingle().Which.ProfilePictureUrl.Should().Be($"/api/v1/blob/profilepicture/image/{imageId}");
    }
}
