using FluentAssertions;
using RealEstate.Core.Entities;
using RealEstate.Core.Specifications;
using Xunit;

namespace RealEstate.Tests.Specifications;

public class BaseSpecificationTests
{
    private class TestProjectSpec : BaseSpecification<Project>
    {
        public TestProjectSpec(string? city, int pageNumber, int pageSize) : base(p => !p.IsDeleted)
        {
            AddCriteriaIf(!string.IsNullOrWhiteSpace(city), p => p.Location.City == city);
            ApplyOrderByDescending(p => p.CreatedAt);
            ApplyPaging(pageNumber, pageSize);
        }
    }

    [Fact]
    public void AddCriteriaIf_WhenConditionFalse_DoesNotAddCriteria()
    {
        var spec = new TestProjectSpec(city: null, pageNumber: 1, pageSize: 10);

        spec.Criteria.Should().HaveCount(1); // only the base !IsDeleted criteria
    }

    [Fact]
    public void AddCriteriaIf_WhenConditionTrue_AddsCriteria()
    {
        var spec = new TestProjectSpec(city: "Ahmedabad", pageNumber: 1, pageSize: 10);

        spec.Criteria.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 20, 40, 20)]
    [InlineData(0, 10, 0, 10)] // page numbers below 1 clamp to the first page
    public void ApplyPaging_ComputesSkipAndTake(int pageNumber, int pageSize, int expectedSkip, int expectedTake)
    {
        var spec = new TestProjectSpec(city: null, pageNumber, pageSize);

        spec.Skip.Should().Be(expectedSkip);
        spec.Take.Should().Be(expectedTake);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void OrderByDescending_IsSetWhenApplied()
    {
        var spec = new TestProjectSpec(city: null, pageNumber: 1, pageSize: 10);

        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
    }
}
