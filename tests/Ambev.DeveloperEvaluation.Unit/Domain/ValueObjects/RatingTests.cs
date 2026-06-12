using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.ValueObjects;

public class RatingTests
{
    [Fact(DisplayName = "Given same values When comparing ratings Then they are equal")]
    public void Rating_SameValues_AreEqual()
    {
        var a = new Rating(4.5m, 120);
        var b = new Rating(4.5m, 120);

        a.Should().Be(b);
    }

    [Fact(DisplayName = "Given different rate When comparing ratings Then they are not equal")]
    public void Rating_DifferentRate_AreNotEqual()
    {
        var a = new Rating(4.5m, 120);
        var b = new Rating(3.0m, 120);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Given different count When comparing ratings Then they are not equal")]
    public void Rating_DifferentCount_AreNotEqual()
    {
        var a = new Rating(4.5m, 120);
        var b = new Rating(4.5m, 99);

        a.Should().NotBe(b);
    }

    [Fact(DisplayName = "Given Rating.Empty When reading properties Then rate and count are zero")]
    public void Rating_Empty_HasZeroValues()
    {
        Rating.Empty.Rate.Should().Be(0m);
        Rating.Empty.Count.Should().Be(0);
    }
}
