using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class ProductControllerTests
{
    private IProductService _mockService = null!;
    private ILogger<ProductController> _mockLogger = null!;
    private ProductController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IProductService>();
        _mockLogger = Substitute.For<ILogger<ProductController>>();
        _sut = new ProductController(_mockService, _mockLogger);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        var items = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "P1", Price = 1.0m, Stock = 5 },
            new Product { Id = Guid.NewGuid(), Name = "P2", Price = 2.0m, Stock = 3 }
        };
        // Controller calls GetAll(skip,take) and GetCount()
        _mockService.GetAll(Arg.Any<int>(), Arg.Any<int>()).Returns(Task.FromResult((IList<Product>)items));
        _mockService.GetCount().Returns(items.Count);

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var expectedDtos = items.Select(p => new ProductDto { Id = p.Id, Name = p.Name, Description = p.Description, Price = p.Price, Stock = p.Stock }).ToList();
        ok!.Value.Should().BeEquivalentTo(new PaginationObject<ProductDto> { Items = expectedDtos, TotalCount = items.Count });
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var prod = new Product { Id = id, Name = "Found", Price = 1.5m, Stock = 10 };
        _mockService.GetProductById(id).Returns(Task.FromResult<Product?>(prod));

        // Act
        var result = await _sut.GetById(id);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var dto = ok!.Value as ProductDto;
        dto.Should().NotBeNull();
        dto!.Should().BeEquivalentTo(new ProductDto { Id = prod.Id, Name = prod.Name, Description = prod.Description, Price = prod.Price, Stock = prod.Stock });
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.GetProductById(id).Returns(Task.FromException<Product?>(new NotFoundException("Product not found")));

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WithValidProduct_ReturnsCreated_And_CallsService()
    {
        // Arrange
        var prodDto = new ProductCreateDto { Name = "New", Price = 2.0m, Stock = 20 };

        // Act
        var result = await _sut.Create(prodDto);

        // Assert
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        var outDto = created!.Value as ProductDto;
        outDto.Should().NotBeNull();
        await _mockService.Received(1).CreateProduct(Arg.Any<Product>());
    }

    [TestMethod]
    public async Task Create_WithNull_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null);

        // Assert
        result.Result.Should().BeOfType<BadRequestResult>();
        await _mockService.DidNotReceive().CreateProduct(Arg.Any<Product>());
    }

    [TestMethod]
    public async Task Update_WithValidProduct_ReturnsNoContent_And_CallsService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var prod = new Product { Id = id, Name = "Upd", Price = 3.0m, Stock = 7 };
        _mockService.GetProductById(id).Returns(Task.FromResult<Product?>(prod));
        var dto = new ProductUpdateDto { Id = id, Name = "Upd", Price = 3.0m, Stock = 7 };

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).UpdateProduct(Arg.Any<Product>());
    }

    [TestMethod]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductUpdateDto { Id = id, Name = "X", Price = 1m, Stock = 1 };
        _mockService.GetProductById(id).Returns(Task.FromException<Product?>(new NotFoundException("Not found")));

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_WhenFound_ReturnsNoContent_And_CallsService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var prod = new Product { Id = id, Name = "X", Price = 1m, Stock = 1 };
        _mockService.GetProductById(id).Returns(Task.FromResult<Product?>(prod));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteProduct(id);
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockService.GetProductById(id).Returns(Task.FromException<Product?>(new NotFoundException("Not found")));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Create_WithInvalidDto_ReturnsBadRequest_And_DoesNotCallService()
    {
        // Arrange: missing name and negative price
        var prodDto = new ProductCreateDto { Name = "", Price = -1.0m, Stock = -5 };

        // Simulate model validation errors (FluentValidation would populate ModelState in runtime)
        _sut.ModelState.AddModelError("Name", "Naam is verplicht.");
        _sut.ModelState.AddModelError("Price", "Prijs moet groter of gelijk aan 0 zijn.");
        _sut.ModelState.AddModelError("Stock", "Voorraad moet groter of gelijk aan 0 zijn.");

        // Act
        var result = await _sut.Create(prodDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _mockService.DidNotReceive().CreateProduct(Arg.Any<Product>());
    }

    [TestMethod]
    public async Task Update_WithInvalidDto_ReturnsBadRequest_And_DoesNotCallService()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Product { Id = id, Name = "Valid", Price = 1.0m, Stock = 5 };
        _mockService.GetProductById(id).Returns(Task.FromResult<Product?>(existing));

        // Invalid update dto: empty name and negative price
        var dto = new ProductUpdateDto { Id = id, Name = "", Price = -10m, Stock = -1 };

        // Simulate model validation errors
        _sut.ModelState.AddModelError("Name", "Naam is verplicht.");
        _sut.ModelState.AddModelError("Price", "Prijs moet groter of gelijk aan 0 zijn.");
        _sut.ModelState.AddModelError("Stock", "Voorraad moet groter of gelijk aan 0 zijn.");

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        await _mockService.DidNotReceive().UpdateProduct(Arg.Any<Product>());
    }
}
