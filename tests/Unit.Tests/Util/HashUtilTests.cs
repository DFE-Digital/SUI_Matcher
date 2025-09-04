using System;
using System.Globalization;

using Shared.Models;
using Shared.Util;

using Xunit;

namespace Unit.Tests.Util;

public class HashUtilTest
{
    [Fact]
    public void StoreUniqueSearchIdFor_ReturnsConsistentHash_ForSameInput()
    {
        var person = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash1 = HashUtil.StoreUniqueSearchIdFor(person);
        var hash2 = "be916a1b542b16fb6f8df9b9f593959d64481bd4ac2a9fcd6ac26b575dad3ab3";
        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreUniqueSearchIdFor_ChangesHash_WhenRelevantFieldChanges()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "john",
            Family = "doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash1 = HashUtil.StoreUniqueSearchIdFor(person1);
        var hash2 = HashUtil.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("1")] // assuming ToGenderFromNumber("1") returns "male"
    [InlineData("")]
    [InlineData(null)]
    public void StoreUniqueSearchIdFor_HandlesGenderVariants(string? genderInput)
    {
        var person = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = genderInput,
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash = HashUtil.StoreUniqueSearchIdFor(person);

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void StoreUniqueSearchIdFor_HandlesMissingGender()
    {
        var person = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var matchResult = new MatchPersonResult
        {
            Given = "John",
            Family = "Doe",
            Gender = "",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var hash = HashUtil.StoreUniqueSearchIdFor(person);
        var hash1 = "11a67a22bfd96862741aaa727373cc9eaf9e90731f582cc9c4f09fa6dc2c604c";
        var hash2 = HashUtil.StoreUniqueSearchIdFor(matchResult);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(hash, hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreUniqueSearchIdFor_HandlesPostalCodeCasingAndWhitespace()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = " ab1   2cd "
        };

        var hash1 = HashUtil.StoreUniqueSearchIdFor(person1);
        var hash2 = HashUtil.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreUniqueSearchIdFor_HandlesDifferentDateInputs()
    {
        var person1 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = DateOnly.Parse("October 21, 2015", CultureInfo.InvariantCulture),
            AddressPostalCode = "AB1 2CD"
        };

        var person2 = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            Gender = "male",
            BirthDate = new DateOnly(2015, 10, 21),
            AddressPostalCode = " ab1   2cd "
        };

        var hash1 = HashUtil.StoreUniqueSearchIdFor(person1);
        var hash2 = HashUtil.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreUniqueSearchIdFor_ReturnsConsistentHash_ForMatchPersonResult()
    {
        var person = new MatchPersonResult
        {
            Given = "Jane",
            Family = "Smith",
            Gender = "female",
            BirthDate = new DateOnly(2004, 5, 15),
            AddressPostalCode = "XY9 8ZW"
        };
        var hash1 = HashUtil.StoreUniqueSearchIdFor(person);
        var hash2 = "2f383866c432df9a75556ed5d732bb8e348b134b8bfc5c5394990af77090c155";

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void StoreUniqueSearchIdFor_HandlesMissingFields_MatchPersonVsPersonSpecification()
    {
        var personSpec = new PersonSpecification
        {
            Given = "Jane",
            Family = "Smith",
            Gender = "female",
            BirthDate = new DateOnly(2004, 5, 15),
            AddressPostalCode = "XY9 8ZW"
        };
        var matchPerson = new MatchPersonResult
        {
            Given = "Jane",
            Family = "Smith",
            Gender = "female",
            BirthDate = new DateOnly(2004, 5, 15),
            AddressPostalCode = "XY9 8ZW"
        };
        var hash1 = HashUtil.StoreUniqueSearchIdFor(personSpec);
        var hash2 = HashUtil.StoreUniqueSearchIdFor(matchPerson);
        Assert.Equal(hash1, hash2);
    }
}