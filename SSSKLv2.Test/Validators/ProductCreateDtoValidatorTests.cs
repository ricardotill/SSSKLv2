using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Validators.Product;

namespace SSSKLv2.Test.Validators;

[TestClass]
public class ProductCreateDtoValidatorTests
{
    private ProductCreateDtoValidator _validator = null!;

    [TestInitialize]
    public void Init()
    {
        _validator = new ProductCreateDtoValidator();
    }

    [TestMethod]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var model = new ProductCreateDto { Name = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Should_Have_Error_When_Name_Too_Long()
    {
        var model = new ProductCreateDto { Name = new string('a', 201) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void Should_Have_Error_When_Price_Negative()
    {
        var model = new ProductCreateDto { Name = "test", Price = -0.01m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestMethod]
    public void Should_Have_Error_When_Price_Has_Too_Many_Decimals()
    {
        var model = new ProductCreateDto { Name = "test", Price = 1.234m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestMethod]
    public void Should_Have_Error_When_Stock_Negative()
    {
        var model = new ProductCreateDto { Name = "test", Stock = -1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Stock);
    }

    [TestMethod]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var model = new ProductCreateDto 
        { 
            Name = "Valid Product", 
            Price = 10.50m, 
            Stock = 100 
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
