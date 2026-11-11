using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Products.Commands.CreateProduct;
using SSSKL.Application.Features.Products.Commands.DeleteProduct;
using SSSKL.Application.Features.Products.Commands.UpdateProduct;
using SSSKL.Application.Features.Products.Queries.GetAllProducts;
using SSSKL.Application.Features.Products.Queries.GetProductById;
using SSSKL.Application.Features.Products.Queries.GetProducts;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class ProductControllerTests
{
    private ISender _sender = null!;
    private ProductController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new ProductController(_sender);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithPagedItems()
    {
        // Arrange
        var page = new PaginationObject<ProductDto>
        {
            Items = [new ProductDto { Id = Guid.NewGuid(), Name = "P1", Price = 1.0m, Stock = 5 }],
            TotalCount = 1
        };
        _sender.Send(new GetProductsQuery(0, 15), Arg.Any<CancellationToken>()).Returns(page);

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(page);
    }

    [TestMethod]
    public async Task GetAll_WithAllTrue_ReturnsOkWithUnpaginatedItems()
    {
        // Arrange
        IReadOnlyList<ProductDto> items =
        [
            new ProductDto { Id = Guid.NewGuid(), Name = "P1", Price = 1.0m, Stock = 5 },
            new ProductDto { Id = Guid.NewGuid(), Name = "P2", Price = 2.0m, Stock = 3 }
        ];
        _sender.Send(Arg.Any<GetAllProductsQuery>(), Arg.Any<CancellationToken>()).Returns(items);

        // Act
        var result = await _sut.GetAll(all: true);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(items);
        await _sender.DidNotReceive().Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductDto { Id = id, Name = "Found", Price = 1.5m, Stock = 10 };
        _sender.Send(new GetProductByIdQuery(id), Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _sender.Send(new GetProductByIdQuery(id), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Product not found"));

        // Act
        var act = () => _sut.GetById(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WithValidProduct_ReturnsCreated_And_SendsCommand()
    {
        // Arrange
        var prodDto = new ProductCreateDto { Name = "New", Price = 2.0m, Stock = 20 };
        var created = new ProductDto { Id = Guid.NewGuid(), Name = "New", Price = 2.0m, Stock = 20 };
        _sender.Send(Arg.Is<CreateProductCommand>(c => c.Product == prodDto), Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _sut.Create(prodDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeSameAs(created);
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [TestMethod]
    public async Task Create_WithNull_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null);

        // Assert
        result.Result.Should().BeOfType<BadRequestResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WithValidProduct_ReturnsNoContent_And_SendsCommand()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductUpdateDto { Id = id, Name = "Upd", Price = 3.0m, Stock = 7 };

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new UpdateProductCommand(id, dto), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductUpdateDto { Id = id, Name = "X", Price = 1m, Stock = 1 };
        _sender.Send(Arg.Any<UpdateProductCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Not found"));

        // Act
        var act = () => _sut.Update(id, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Delete_ReturnsNoContent_And_SendsCommand()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new DeleteProductCommand(id), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_WhenNotFound_PropagatesNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _sender.Send(Arg.Any<DeleteProductCommand>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Not found"));

        // Act
        var act = () => _sut.Delete(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WithInvalidDto_ReturnsBadRequest_And_DoesNotSendCommand()
    {
        // Arrange: missing name and negative price
        var prodDto = new ProductCreateDto { Name = "", Price = -1.0m, Stock = -5 };

        // Simulate model validation errors (FluentValidation would populate ModelState in runtime)
        _sut.ModelState.AddModelError("Name", "Naam is verplicht.");
        _sut.ModelState.AddModelError("Price", "Prijs moet groter of gelijk aan 0 zijn.");

        // Act
        var result = await _sut.Create(prodDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Update_WithInvalidDto_ReturnsBadRequest_And_DoesNotSendCommand()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ProductUpdateDto { Id = id, Name = "", Price = -10m, Stock = -1 };
        _sut.ModelState.AddModelError("Name", "Naam is verplicht.");

        // Act
        var result = await _sut.Update(id, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<UpdateProductCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Create_WhenHandlerThrows_PropagatesToGlobalHandler()
    {
        // Arrange
        var prodDto = new ProductCreateDto { Name = "New", Price = 2.0m, Stock = 20 };
        _sender.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>()).Throws(new Exception("Fail"));

        // Act
        var act = () => _sut.Create(prodDto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Fail");
    }
}
