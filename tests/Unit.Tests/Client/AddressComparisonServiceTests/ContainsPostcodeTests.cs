using SUI.Client.Core.Services;

namespace Unit.Tests.Client.AddressComparisonServiceTests;

public class ContainsPostcodeTests
{
    [Fact]
    public void ShouldReturnTrue_WhenPostcodeIsContained()
    {
        // Arrange
        const string address = "1~York place~York~YO1 6GA|2~York place~York~YO1 6GB";
        const string postcode = "YO1 6GA";

        // Act
        var result = AddressComparisonService.ContainsPostcode(address, postcode);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("1~York place~York~YO1 6GA|2~York place~York~YO1 6GB", "YO1 6GC")]
    [InlineData("", "YO1 6GC")] // No address to check against
    [InlineData("1~York place~York~YO1 6GA|2~York place~York~YO1 6GB", "")]
    public void ShouldReturnFalse_WhenPostcodeIsNotContained(string address, string postcode)
    {
        // Arrange
        // Act
        var result = AddressComparisonService.ContainsPostcode(address, postcode);

        // Assert
        Assert.False(result);
    }
}