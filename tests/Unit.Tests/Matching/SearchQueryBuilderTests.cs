using MatchingApi.Search;

using Shared.Models;

namespace Unit.Tests.Matching;

public class SearchQueryBuilderTests
{
    [Theory]
    [InlineData("AB12 3CD", "AB*")]
    [InlineData("AB123CD", "AB*")]
    public void AddsPostcodeWildcardCorrectly(string postcode, string expected)
    {
        var builder = new SearchQueryBuilder(new SearchSpecification
        {
            BirthDate = DateOnly.FromDateTime(DateTime.Now),
            AddressPostalCode = postcode
        });

        builder.AddFuzzyGfdRangePostcodeWildcard();
        
        var result = builder.Build();

        var query = result.ContainsKey("FuzzyGFDRangePostcodeWildcard");
        Assert.True(query);
        Assert.Equal(expected, result.Values.First().AddressPostalcode);
    }
    
    [Fact]
    public void ShouldIncludeHistoryOnNonFuzzyQueries()
    {
        var builder = new SearchQueryBuilder(new SearchSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.FromDateTime(new DateTime(2010, 1, 1))
        });

        builder.AddNonFuzzyGfd();
        builder.AddNonFuzzyGfdRange();
        builder.AddNonFuzzyAll();
        
        var result = builder.Build();

        foreach (var query in result.Values)
        {
            Assert.True(query.History);
        }
    }
}