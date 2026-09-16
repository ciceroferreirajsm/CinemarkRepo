using FluentAssertions;
using MovieCatalog.Application.Common;
using Xunit;

namespace MovieCatalog.UnitTests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(20, 10, 2)]
    [InlineData(0, 10, 0)]
    public void TotalPages_IsComputedFromTotalCountAndPageSize(long totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<string> { TotalCount = totalCount, PageSize = pageSize };

        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void TotalPages_WhenPageSizeIsZero_ReturnsZeroInsteadOfThrowing()
    {
        var result = new PagedResult<string> { TotalCount = 10, PageSize = 0 };

        result.TotalPages.Should().Be(0);
    }
}
