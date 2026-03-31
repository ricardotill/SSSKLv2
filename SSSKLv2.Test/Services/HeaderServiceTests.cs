using FluentAssertions;
using SSSKLv2.Services;

namespace SSSKLv2.Test.Services;

[TestClass]
public class HeaderServiceTests
{
    private HeaderService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sut = new HeaderService();
    }

    [TestMethod]
    public void SetName_ShouldUpdateNameAndInvokeHeaderChanged()
    {
        // Arrange
        var name = "John Doe";
        bool eventRaised = false;
        _sut.HeaderChanged += (s, e) => eventRaised = true;

        // Act
        _sut.SetName(name);

        // Assert
        _sut.Name.Should().Be(name);
        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public void SetSaldo_ShouldUpdateSaldoAndInvokeHeaderChanged()
    {
        // Arrange
        var saldo = "€ 10,00";
        bool eventRaised = false;
        _sut.HeaderChanged += (s, e) => eventRaised = true;

        // Act
        _sut.SetSaldo(saldo);

        // Assert
        _sut.Saldo.Should().Be(saldo);
        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public void NotifyHeaderChanged_ShouldInvokeHeaderChanged()
    {
        // Arrange
        bool eventRaised = false;
        _sut.HeaderChanged += (s, e) => eventRaised = true;

        // Act
        _sut.NotifyHeaderChanged();

        // Assert
        eventRaised.Should().BeTrue();
    }
}
