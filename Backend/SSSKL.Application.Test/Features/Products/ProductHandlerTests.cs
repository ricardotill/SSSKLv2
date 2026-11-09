using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Products;
using SSSKL.Application.Features.Products.Commands.CreateProduct;
using SSSKL.Application.Features.Products.Commands.DeleteProduct;
using SSSKL.Application.Features.Products.Commands.UpdateProduct;
using SSSKL.Application.Features.Products.Queries.GetAllProducts;
using SSSKL.Application.Features.Products.Queries.GetAvailableProducts;
using SSSKL.Application.Features.Products.Queries.GetProductById;
using SSSKL.Application.Features.Products.Queries.GetProducts;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Products;

[TestClass]
public class ProductHandlerTests
{
    private IProductRepository _mockRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<IProductRepository>();
    }

    #region GetProductById Tests

    [TestMethod]
    public async Task GetProductById_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = CreateProduct(productId, "Test Product", 10.99m);
        _mockRepository.GetById(productId).Returns(expectedProduct);
        var sut = new GetProductByIdHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedProduct.ToDto());
        await _mockRepository.Received(1).GetById(productId);
    }

    [TestMethod]
    public async Task GetProductById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Throws(new NotFoundException("Product not found"));
        var sut = new GetProductByIdHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
    }

    [TestMethod]
    public async Task GetProductById_WhenRepositoryThrowsUnexpectedException_ShouldPropagateException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Throws(new InvalidOperationException("Database connection error"));
        var sut = new GetProductByIdHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
    }

    #endregion

    #region GetAllProducts / GetProducts Tests

    [TestMethod]
    public async Task GetAllProducts_ShouldReturnAllProductsFromRepository()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct(Guid.NewGuid(), "Product 1", 10.99m),
            CreateProduct(Guid.NewGuid(), "Product 2", 20.50m)
        };
        _mockRepository.GetAll().Returns(products);
        var sut = new GetAllProductsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAllProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(products.Select(p => p.ToDto()));
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAllProducts_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.GetAll().Returns(new List<Product>());
        var sut = new GetAllProductsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAllProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllProducts_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRepository.GetAll().Throws(new InvalidOperationException("Database connection error"));
        var sut = new GetAllProductsHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetAllProductsQuery(), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
    }

    [TestMethod]
    public async Task GetProducts_Paged_ShouldReturnPagedProductsAndTotalCount()
    {
        // Arrange
        var products = new List<Product> { CreateProduct(Guid.NewGuid(), "Product 1", 10.99m) };
        _mockRepository.GetAll(10, 5).Returns(products);
        _mockRepository.GetCount().Returns(42);
        var sut = new GetProductsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetProductsQuery(10, 5), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new PaginationObject<ProductDto>
        {
            Items = products.Select(p => p.ToDto()).ToList(),
            TotalCount = 42
        });
        await _mockRepository.Received(1).GetAll(10, 5);
    }

    #endregion

    #region GetAvailableProducts Tests

    [TestMethod]
    public async Task GetAvailableProducts_ShouldReturnAllAvailableProductsFromRepository()
    {
        // Arrange
        var availableProducts = new List<Product>
        {
            CreateProduct(Guid.NewGuid(), "Available Product 1", 10.99m, 5),
            CreateProduct(Guid.NewGuid(), "Available Product 2", 20.50m, 3)
        };
        _mockRepository.GetAllAvailable().Returns(availableProducts);
        var sut = new GetAvailableProductsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAvailableProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(availableProducts.Select(p => p.ToDto()));
        await _mockRepository.Received(1).GetAllAvailable();
    }

    [TestMethod]
    public async Task GetAvailableProducts_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.GetAllAvailable().Returns(new List<Product>());
        var sut = new GetAvailableProductsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetAvailableProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAvailableProducts_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRepository.GetAllAvailable().Throws(new InvalidOperationException("Database connection error"));
        var sut = new GetAvailableProductsHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetAvailableProductsQuery(), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
    }

    #endregion

    #region CreateProduct Tests

    [TestMethod]
    public async Task CreateProduct_WithValidProduct_ShouldCallRepositoryAndReturnDto()
    {
        // Arrange
        var dto = new ProductCreateDto { Name = "New Product", Description = null, Price = 15.75m, Stock = 4, EnableLeaderboard = true };
        var sut = new CreateProductHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new CreateProductCommand(dto), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Create(Arg.Is<Product>(p =>
            p.Name == "New Product" && p.Description == string.Empty && p.Price == 15.75m && p.Stock == 4 && p.EnableLeaderboard));
        result.Name.Should().Be("New Product");
        result.Price.Should().Be(15.75m);
    }

    [TestMethod]
    public async Task CreateProduct_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var dto = new ProductCreateDto { Name = "New Product", Price = 15.75m, Stock = 1 };
        _mockRepository.Create(Arg.Any<Product>()).Throws(new InvalidOperationException("Duplicate product name"));
        var sut = new CreateProductHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new CreateProductCommand(dto), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Duplicate product name");
    }

    #endregion

    #region UpdateProduct Tests

    [TestMethod]
    public async Task UpdateProduct_WithValidProduct_ShouldApplyValuesAndCallRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = CreateProduct(id, "Old", 1m);
        _mockRepository.GetById(id).Returns(existing);
        var dto = new ProductUpdateDto { Id = id, Name = "Updated Product", Description = "New", Price = 25.99m, Stock = 7, EnableLeaderboard = true };
        var sut = new UpdateProductHandler(_mockRepository);

        // Act
        await sut.Handle(new UpdateProductCommand(id, dto), CancellationToken.None);

        // Assert
        existing.Name.Should().Be("Updated Product");
        existing.Description.Should().Be("New");
        existing.Price.Should().Be(25.99m);
        existing.Stock.Should().Be(7);
        existing.EnableLeaderboard.Should().BeTrue();
        await _mockRepository.Received(1).Update(existing);
    }

    [TestMethod]
    public async Task UpdateProduct_WhenProductDoesNotExist_ShouldPropagateNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Throws(new NotFoundException("Product not found"));
        var dto = new ProductUpdateDto { Id = id, Name = "Non-existent Product", Price = 15.75m, Stock = 1 };
        var sut = new UpdateProductHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new UpdateProductCommand(id, dto), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockRepository.DidNotReceive().Update(Arg.Any<Product>());
    }

    [TestMethod]
    public async Task UpdateProduct_WhenRepositoryThrowsInvalidOperationException_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns(CreateProduct(id, "Conflicting Product", 15.75m));
        _mockRepository.Update(Arg.Any<Product>()).Throws(new InvalidOperationException("Concurrency conflict occurred"));
        var dto = new ProductUpdateDto { Id = id, Name = "Conflicting Product", Price = 15.75m, Stock = 1 };
        var sut = new UpdateProductHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new UpdateProductCommand(id, dto), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Concurrency conflict occurred");
    }

    #endregion

    #region DeleteProduct Tests

    [TestMethod]
    public async Task DeleteProduct_WithValidId_ShouldCallRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Returns(CreateProduct(productId, "P", 1m));
        var sut = new DeleteProductHandler(_mockRepository);

        // Act
        await sut.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Delete(productId);
    }

    [TestMethod]
    public async Task DeleteProduct_WhenProductDoesNotExist_ShouldPropagateNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Throws(new NotFoundException("Product not found"));
        var sut = new DeleteProductHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockRepository.DidNotReceive().Delete(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task DeleteProduct_WhenRepositoryDeleteThrows_ShouldPropagateException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Returns(CreateProduct(productId, "P", 1m));
        _mockRepository.Delete(productId).Throws(new ArgumentException("Invalid product ID"));
        var sut = new DeleteProductHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid product ID");
    }

    #endregion

    #region Helper Methods

    private static Product CreateProduct(Guid id, string name, decimal price, int stock = 10)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Price = price,
            Stock = stock,
            CreatedOn = DateTime.UtcNow
        };
    }

    #endregion
}
