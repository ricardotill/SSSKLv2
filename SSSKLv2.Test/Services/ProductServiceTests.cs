using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SSSKLv2.Test.Services;

[TestClass]
public class ProductServiceTests
{
    private IProductRepository _mockRepository = null!;
    private ILogger<ProductService> _mockLogger = null!;
    private ProductService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<IProductRepository>();
        _mockLogger = Substitute.For<ILogger<ProductService>>();
        _sut = new ProductService(_mockRepository, _mockLogger);
    }

    #region GetProductById Tests

    [TestMethod]
    public async Task GetProductById_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = CreateProduct(productId, "Test Product", 10.99m);
        
        _mockRepository.GetById(productId).Returns(expectedProduct);

        // Act
        var result = await _sut.GetProductById(productId);

        // Assert
        result.Should().BeEquivalentTo(expectedProduct);
        await _mockRepository.Received(1).GetById(productId);
    }

    [TestMethod]
    public async Task GetProductById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.GetById(productId).Throws(new NotFoundException("Product not found"));

        // Act
        Func<Task> action = async () => await _sut.GetProductById(productId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockRepository.Received(1).GetById(productId);
    }

    [TestMethod]
    public async Task GetProductById_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockRepository.GetById(emptyGuid).Throws(new ArgumentException("Invalid product ID"));

        // Act
        Func<Task> action = async () => await _sut.GetProductById(emptyGuid);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid product ID");
        await _mockRepository.Received(1).GetById(emptyGuid);
    }

    [TestMethod]
    public async Task GetProductById_WhenRepositoryThrowsUnexpectedException_ShouldPropagateException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Database connection error");
        _mockRepository.GetById(productId).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.GetProductById(productId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
        await _mockRepository.Received(1).GetById(productId);
    }

    #endregion

    #region GetAll Tests

    [TestMethod]
    public async Task GetAll_ShouldReturnAllProductsFromRepository()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct(Guid.NewGuid(), "Product 1", 10.99m),
            CreateProduct(Guid.NewGuid(), "Product 2", 20.50m)
        };

        _mockRepository.GetAll().Returns(products);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeEquivalentTo(products);
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAll_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.GetAll().Returns(new List<Product>());

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeEmpty();
        await _mockRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAll_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection error");
        _mockRepository.GetAll().Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.GetAll();

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
        await _mockRepository.Received(1).GetAll();
    }

    #endregion

    #region GetAllAvailable Tests

    [TestMethod]
    public async Task GetAllAvailable_ShouldReturnAllAvailableProductsFromRepository()
    {
        // Arrange
        var availableProducts = new List<Product>
        {
            CreateProduct(Guid.NewGuid(), "Available Product 1", 10.99m, 5),
            CreateProduct(Guid.NewGuid(), "Available Product 2", 20.50m, 3)
        };

        _mockRepository.GetAllAvailable().Returns(availableProducts);

        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().BeEquivalentTo(availableProducts);
        await _mockRepository.Received(1).GetAllAvailable();
    }

    [TestMethod]
    public async Task GetAllAvailable_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.GetAllAvailable().Returns(new List<Product>());

        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().BeEmpty();
        await _mockRepository.Received(1).GetAllAvailable();
    }

    [TestMethod]
    public async Task GetAllAvailable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection error");
        _mockRepository.GetAllAvailable().Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.GetAllAvailable();

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection error");
        await _mockRepository.Received(1).GetAllAvailable();
    }

    #endregion

    #region CreateProduct Tests

    [TestMethod]
    public async Task CreateProduct_WithValidProduct_ShouldCallRepository()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "New Product", 15.75m);

        // Act
        await _sut.CreateProduct(product);

        // Assert
        await _mockRepository.Received(1).Create(product);
    }

    [TestMethod]
    public async Task CreateProduct_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "New Product", 15.75m);
        var expectedException = new DbUpdateException("Duplicate product name");
        _mockRepository.Create(product).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.CreateProduct(product);

        // Assert
        await action.Should().ThrowAsync<DbUpdateException>().WithMessage("Duplicate product name");
        await _mockRepository.Received(1).Create(product);
    }

    #endregion

    #region UpdateProduct Tests

    [TestMethod]
    public async Task UpdateProduct_WithValidProduct_ShouldCallRepository()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Updated Product", 25.99m);

        // Act
        await _sut.UpdateProduct(product);

        // Assert
        await _mockRepository.Received(1).Update(product);
    }

    [TestMethod]
    public async Task UpdateProduct_WhenRepositoryThrowsNotFoundException_ShouldPropagateException()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Non-existent Product", 15.75m);
        var expectedException = new NotFoundException("Product not found");
        _mockRepository.Update(product).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.UpdateProduct(product);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockRepository.Received(1).Update(product);
    }

    [TestMethod]
    public async Task UpdateProduct_WhenRepositoryThrowsDbUpdateConcurrencyException_ShouldPropagateException()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Conflicting Product", 15.75m);
        var expectedException = new DbUpdateConcurrencyException("Concurrency conflict occurred");
        _mockRepository.Update(product).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.UpdateProduct(product);

        // Assert
        await action.Should().ThrowAsync<DbUpdateConcurrencyException>().WithMessage("Concurrency conflict occurred");
        await _mockRepository.Received(1).Update(product);
    }

    #endregion

    #region DeleteProduct Tests

    [TestMethod]
    public async Task DeleteProduct_WithValidId_ShouldCallRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        await _sut.DeleteProduct(productId);

        // Assert
        await _mockRepository.Received(1).Delete(productId);
    }

    [TestMethod]
    public async Task DeleteProduct_WithEmptyGuid_ShouldPropagateException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        var expectedException = new ArgumentException("Invalid product ID");
        _mockRepository.Delete(emptyGuid).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.DeleteProduct(emptyGuid);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid product ID");
        await _mockRepository.Received(1).Delete(emptyGuid);
    }

    [TestMethod]
    public async Task DeleteProduct_WhenRepositoryThrowsNotFoundException_ShouldPropagateException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedException = new NotFoundException("Product not found");
        _mockRepository.Delete(productId).Throws(expectedException);

        // Act
        Func<Task> action = async () => await _sut.DeleteProduct(productId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockRepository.Received(1).Delete(productId);
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