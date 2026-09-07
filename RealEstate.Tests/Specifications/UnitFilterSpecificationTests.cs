using FluentAssertions;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Units.Specifications;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;
using Xunit;

namespace RealEstate.Tests.Specifications;

public class UnitFilterSpecificationTests
{
    private static Unit MakeUnit(
        UnitType type = UnitType.TwoBhk,
        decimal price = 5_000_000,
        string unitNumber = "A-401",
        string projectName = "Sunrise Towers",
        string city = "Mumbai",
        string propertyName = "Block A") => new()
    {
        Type = type,
        Price = price,
        UnitNumber = unitNumber,
        ProjectSnapshot = new ProjectSnapshot { Name = projectName, City = city },
        PropertySnapshot = new PropertySnapshot { Name = propertyName, Type = "Residential" },
    };

    // The search criteria is a single combined predicate (OR across fields, or a price range, or
    // a type match) added on top of the base !IsDeleted criteria -- compiling and running it
    // directly against in-memory Unit objects exercises the exact same expression the Mongo LINQ
    // provider would translate, without needing a real database.
    private static bool Matches(string search, Unit unit)
    {
        var spec = new UnitFilterSpecification(new UnitQueryParams { Search = search, PageSize = 20 });
        var searchCriteria = spec.Criteria.Last();
        return searchCriteria.Compile()(unit);
    }

    [Theory]
    [InlineData("2bhk")]
    [InlineData("2 bhk")]
    [InlineData("2-BHK")]
    [InlineData("two bhk")]
    public void Search_BhkDigitOrWordForm_MatchesOnlyThatType(string term)
    {
        Matches(term, MakeUnit(UnitType.TwoBhk)).Should().BeTrue();
        Matches(term, MakeUnit(UnitType.ThreeBhk)).Should().BeFalse();
    }

    [Theory]
    [InlineData("studio", UnitType.Studio)]
    [InlineData("Penthouse", UnitType.Penthouse)]
    [InlineData("villa", UnitType.Villa)]
    public void Search_NamedUnitType_MatchesExactType(string term, UnitType type)
    {
        Matches(term, MakeUnit(type)).Should().BeTrue();
        Matches(term, MakeUnit(type == UnitType.Villa ? UnitType.Studio : UnitType.Villa)).Should().BeFalse();
    }

    [Fact]
    public void Search_NumericQuery_MatchesUnitsWithinFifteenPercentOfTargetPrice()
    {
        const string term = "5000000";

        Matches(term, MakeUnit(price: 5_000_000)).Should().BeTrue(); // exact
        Matches(term, MakeUnit(price: 5_600_000)).Should().BeTrue(); // +12%, within range
        Matches(term, MakeUnit(price: 4_300_000)).Should().BeTrue(); // -14%, within range
        Matches(term, MakeUnit(price: 7_000_000)).Should().BeFalse(); // +40%, out of range
        Matches(term, MakeUnit(price: 51_000_000)).Should().BeFalse(); // digit-substring match, not a price match
    }

    [Theory]
    [InlineData("mumbai")]
    [InlineData("MUMBAI")]
    [InlineData("sunrise")]
    [InlineData("block a")]
    [InlineData("a-401")]
    public void Search_FreeText_MatchesCityOrProjectOrPropertyOrUnitNumberSubstring(string term)
    {
        Matches(term, MakeUnit()).Should().BeTrue();
    }

    [Fact]
    public void Search_FreeText_DoesNotMatchUnrelatedUnit()
    {
        Matches("bangalore", MakeUnit()).Should().BeFalse();
    }

    [Fact]
    public void Search_BlankOrMissing_AddsNoExtraCriteria()
    {
        var spec = new UnitFilterSpecification(new UnitQueryParams { Search = "   ", PageSize = 20 });

        spec.Criteria.Should().HaveCount(1); // only the base !IsDeleted criteria
    }
}
