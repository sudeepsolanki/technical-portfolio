using FluentAssertions;
using ProductCatalog.Application.Common;

namespace ProductCatalog.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_Should_Return_IsSuccess_True()
    {
        var result = Result<string>.Success("value");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_Should_Return_IsSuccess_False()
    {
        var result = Result<string>.Failure("error message");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("error message");
        result.Value.Should().BeNull();
    }
}

public class PagedResultTests
{
    [Fact]
    public void TotalPages_Should_Calculate_Correctly()
    {
        var paged = new PagedResult<int>
        {
            Items = new List<int>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        paged.TotalPages.Should().Be(3);
    }

    [Fact]
    public void HasPreviousPage_Should_Be_False_For_First_Page()
    {
        var paged = new PagedResult<int> { Page = 1, PageSize = 10, TotalCount = 25 };
        paged.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_Should_Be_True_When_More_Items()
    {
        var paged = new PagedResult<int> { Page = 1, PageSize = 10, TotalCount = 25 };
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_Should_Be_False_On_Last_Page()
    {
        var paged = new PagedResult<int> { Page = 3, PageSize = 10, TotalCount = 25 };
        paged.HasNextPage.Should().BeFalse();
    }
}
