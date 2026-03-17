using SUI.Client.Core.Application.UseCases.ReconcilePeople;
using SUI.Client.Core.Domain.Models;

namespace Unit.Tests.Client.DomainTests;

public class AddressHistoryTests
{
    [Fact]
    public void PrimaryAddressSameAs_ShouldMatch_WhenBasicAddressAndFirstLineMatches()
    {
        // Arrange
        var addrPrime = new AddressMinimal("12", "York street", "YO16GA");
        var addrHistory = new AddressMinimal("12", "York street", "YO16GA");

        var addr2Prime = new AddressMinimal("12", "York street", "YO16GA");
        var addr2History = new AddressMinimal("12", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Matched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUnmatched_WhenOtherHasDifferentBuildingNumber()
    {
        // Arrange
        var addrPrime = new AddressMinimal("12", "York street", "YO16GA");
        var addrHistory = new AddressMinimal("12", "York street", "YO16GA");

        var addr2Prime = new AddressMinimal("13", "York street", "YO16GA");
        var addr2History = new AddressMinimal("13", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Unmatched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldMatch_WhenFlatAddressExistsOnOneSide()
    {
        // Arrange
        var addrPrime = new AddressMinimal("Flat 1", "12 York street", "YO16GA");
        var addrHistory = new AddressMinimal("Flat 1", "12 York street", "YO16GA");

        var addr2Prime = new AddressMinimal("12", "York street", "YO16GA");
        var addr2History = new AddressMinimal("12", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Uncertain, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeMatch_WhenNumberExistsInBothAddressLines()
    {
        var addrPrime = new AddressMinimal("Flat 1", "12 York street", "YO16GA");
        var addrHistory = new AddressMinimal("Flat 1", "12 York street", "YO16GA");

        var addr2Prime = new AddressMinimal("Flat 1", "12 York street", "YO16GA");
        var addr2History = new AddressMinimal("Flat 1", "12 York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Matched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeMatch_WhenNumberExistsOnLine2()
    {
        var addrPrime = new AddressMinimal("Some house name", "12 York street", "YO16GA");
        var addrHistory = new AddressMinimal("Some house name", "12 York street", "YO16GA");

        var addr2Prime = new AddressMinimal("Some house name somewhere", "12 York street", "YO16GA");
        var addr2History = new AddressMinimal("Some house name somewhere", "12 York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Matched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUnmatched_WhenNotEnoughInformationExists()
    {
        var addrPrime = new AddressMinimal("", "York street", "YO16GA");
        var addrHistory = new AddressMinimal("", "York street", "YO16GA");

        var addr2Prime = new AddressMinimal("12", "York street", "YO16GA");
        var addr2History = new AddressMinimal("12", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Unmatched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUnmatched_WhenNotEnoughInformationExistsOnBoth()
    {
        // Super edge case
        var addrPrime = new AddressMinimal("some house", "bob str", "YO16GA");
        var addrHistory = new AddressMinimal("some house", "bob str", "YO16GA");

        var addr2Prime = new AddressMinimal("some house somewhere", "bob str", "YO16GA");
        var addr2History = new AddressMinimal("some house somewhere", "bob str", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Unmatched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUnmatched_WhenOnlyOneAddressHasLine2Number()
    {
        // Arrange - Line1 has no numbers on both, but only one has a Line2 number
        var addrPrime = new AddressMinimal("Some house", "12 York street", "YO16GA");
        var addrHistory = new AddressMinimal("Some house", "12 York street", "YO16GA");

        var addr2Prime = new AddressMinimal("Some house somewhere", "York street", "YO16GA");
        var addr2History = new AddressMinimal("Some house somewhere", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Unmatched, result.Status);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUncertain_WhenOneAddressHasNumbersInBothLinesAndOtherHasSingleNumberThatMatches()
    {
        // Arrange - a1 has numbers in both lines (Flat 1, 12 York street) and a2 has only one number (12) that matches a1's Line2 number
        var addrPrime = new AddressMinimal("1", "12 York street", "YO16GA");
        var addrHistory = new AddressMinimal("1", "12 York street", "YO16GA");

        var addr2Prime = new AddressMinimal("12", "York street", "YO16GA");
        var addr2History = new AddressMinimal("12", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Uncertain, result.Status);
        Assert.Equal(AddressComparisonResult.AddressMatchReason.FlatMissing, result.Reason);
    }

    [Fact]
    public void PrimaryAddressSameAs_ShouldBeUncertain_WhenOneAddressHasNumbersInBothLinesAndOtherHasSingleNumberThatMatches_Reverse()
    {
        // Arrange - a1 has numbers in both lines (Flat 1, 12 York street) and a2 has only one number (12) that matches a1's Line2 number
        var addr2Prime = new AddressMinimal("1", "12 York street", "YO16GA");
        var addr2History = new AddressMinimal("1", "12 York street", "YO16GA");

        var addrPrime = new AddressMinimal("12", "York street", "YO16GA");
        var addrHistory = new AddressMinimal("12", "York street", "YO16GA");

        // Act
        var sut = new AddressHistory([addrHistory], addrPrime);
        var addr2Sut = new AddressHistory([addr2History], addr2Prime);

        var result = sut.PrimaryAddressSameAs(addr2Sut);

        // Assert
        Assert.Equal(AddressComparisonResult.AddressMatchStatus.Uncertain, result.Status);
        Assert.Equal(AddressComparisonResult.AddressMatchReason.FlatMissing, result.Reason);
    }
}