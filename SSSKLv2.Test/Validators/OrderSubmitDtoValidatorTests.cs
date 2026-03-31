using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Validators.Order;

namespace SSSKLv2.Test.Validators;

[TestClass]
public class OrderSubmitDtoValidatorTests
{
    private OrderSubmitDtoValidator _validator = null!;

    [TestInitialize]
    public void Init()
    {
        _validator = new OrderSubmitDtoValidator();
    }

    [TestMethod]
    public void Should_Have_Error_When_Products_Empty()
    {
        var model = new OrderSubmitDto { Products = new List<Guid>() };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Products);
    }

    [TestMethod]
    public void Should_Have_Error_When_Products_Null()
    {
        var model = new OrderSubmitDto { Products = null! };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Products);
    }

    [TestMethod]
    public void Should_Have_Error_When_Users_Empty()
    {
        var model = new OrderSubmitDto { Users = new List<Guid>() };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Users);
    }

    [TestMethod]
    public void Should_Have_Error_When_Users_Null()
    {
        var model = new OrderSubmitDto { Users = null! };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Users);
    }

    [TestMethod]
    public void Should_Have_Error_When_Amount_Less_Than_1()
    {
        var model = new OrderSubmitDto 
        { 
            Products = new List<Guid> { Guid.NewGuid() }, 
            Users = new List<Guid> { Guid.NewGuid() }, 
            Amount = 0 
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [TestMethod]
    public void Should_Have_Error_When_Amount_Too_Large()
    {
        var model = new OrderSubmitDto 
        { 
            Products = new List<Guid> { Guid.NewGuid() }, 
            Users = new List<Guid> { Guid.NewGuid() }, 
            Amount = 1001 
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [TestMethod]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var model = new OrderSubmitDto 
        { 
            Products = new List<Guid> { Guid.NewGuid() }, 
            Users = new List<Guid> { Guid.NewGuid() }, 
            Amount = 1,
            Split = false
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
