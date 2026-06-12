using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.ValueObjects;

public class AddressTests
{
    [Fact(DisplayName = "Given same values When comparing addresses Then they are equal")]
    public void Address_SameValues_AreEqual()
    {
        var a = new Address("São Paulo", "Av. Paulista", 1000, "01310-100", "-23.561", "-46.656");
        var b = new Address("São Paulo", "Av. Paulista", 1000, "01310-100", "-23.561", "-46.656");

        a.Should().Be(b);
    }

    [Fact(DisplayName = "Given different values When comparing addresses Then they are not equal")]
    public void Address_DifferentCity_AreNotEqual()
    {
        var a = new Address("São Paulo", "Av. Paulista", 1000, "01310-100", "-23.561", "-46.656");
        var b = new Address("Rio de Janeiro", "Av. Paulista", 1000, "01310-100", "-23.561", "-46.656");

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Given address When reading properties Then values match constructor args")]
    public void Address_Properties_MatchConstructorArgs()
    {
        var address = new Address("Campinas", "Rua XV", 42, "13010-000", "-22.905", "-47.060");

        address.City.Should().Be("Campinas");
        address.Street.Should().Be("Rua XV");
        address.Number.Should().Be(42);
        address.Zipcode.Should().Be("13010-000");
        address.Lat.Should().Be("-22.905");
        address.Long.Should().Be("-47.060");
    }

    [Fact(DisplayName = "Given Address.Empty When reading properties Then all values are default")]
    public void Address_Empty_HasDefaultValues()
    {
        Address.Empty.City.Should().BeEmpty();
        Address.Empty.Street.Should().BeEmpty();
        Address.Empty.Number.Should().Be(0);
        Address.Empty.Zipcode.Should().BeEmpty();
    }
}
