using System.Globalization;

namespace Unit.Tests.SharedTests.ServiceTests.ActivityHashServiceTests;

public class StoreUniqueSearchIdForTests
{
    [Fact]
    public void Should_ReturnConsistentHash_When_PersonSpecificationInputIsTheSame()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person = ActivityHashServiceTestHarness.CreatePersonSpecification();

        var hash = harness.Service.StoreUniqueSearchIdFor(person);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal("be916a1b542b16fb6f8df9b9f593959d64481bd4ac2a9fcd6ac26b575dad3ab3", hash);
    }

    [Fact]
    public void Should_ReturnSameHash_When_NameCasingChanges()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person1 = ActivityHashServiceTestHarness.CreatePersonSpecification();
        var person2 = ActivityHashServiceTestHarness.CreatePersonSpecification(
            given: "john",
            family: "doe"
        );

        var hash1 = harness.Service.StoreUniqueSearchIdFor(person1);
        var hash2 = harness.Service.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_ReturnHash_When_GenderHasVariantValue(string? genderInput)
    {
        var harness = new ActivityHashServiceTestHarness();
        var person = ActivityHashServiceTestHarness.CreatePersonSpecification(gender: genderInput);

        var hash = harness.Service.StoreUniqueSearchIdFor(person);

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Should_ReturnSameHashForSpecificationAndResult_When_GenderIsMissing()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person = ActivityHashServiceTestHarness.CreatePersonSpecification(gender: "");
        var matchResult = ActivityHashServiceTestHarness.CreateMatchPersonResult(
            given: "John",
            family: "Doe",
            gender: "",
            birthDate: new DateOnly(1990, 1, 1),
            addressPostalCode: "AB1 2CD"
        );

        var personHash = harness.Service.StoreUniqueSearchIdFor(person);
        var resultHash = harness.Service.StoreUniqueSearchIdFor(matchResult);

        Assert.False(string.IsNullOrWhiteSpace(personHash));
        Assert.Equal(
            "11a67a22bfd96862741aaa727373cc9eaf9e90731f582cc9c4f09fa6dc2c604c",
            personHash
        );
        Assert.Equal(personHash, resultHash);
    }

    [Fact]
    public void Should_ReturnSameHash_When_PostalCodeCasingAndWhitespaceChanges()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person1 = ActivityHashServiceTestHarness.CreatePersonSpecification();
        var person2 = ActivityHashServiceTestHarness.CreatePersonSpecification(
            addressPostalCode: " ab1   2cd "
        );

        var hash1 = harness.Service.StoreUniqueSearchIdFor(person1);
        var hash2 = harness.Service.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Should_ReturnSameHash_When_DateInputsRepresentSameDate()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person1 = ActivityHashServiceTestHarness.CreatePersonSpecification(
            birthDate: DateOnly.Parse("October 21, 2015", CultureInfo.InvariantCulture)
        );
        var person2 = ActivityHashServiceTestHarness.CreatePersonSpecification(
            birthDate: new DateOnly(2015, 10, 21),
            addressPostalCode: " ab1   2cd "
        );

        var hash1 = harness.Service.StoreUniqueSearchIdFor(person1);
        var hash2 = harness.Service.StoreUniqueSearchIdFor(person2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Should_ReturnConsistentHash_When_MatchPersonResultInputIsTheSame()
    {
        var harness = new ActivityHashServiceTestHarness();
        var person = ActivityHashServiceTestHarness.CreateMatchPersonResult();

        var hash = harness.Service.StoreUniqueSearchIdFor(person);

        Assert.Equal("2f383866c432df9a75556ed5d732bb8e348b134b8bfc5c5394990af77090c155", hash);
    }

    [Fact]
    public void Should_ReturnSameHashForSpecificationAndResult_When_FieldsMatch()
    {
        var harness = new ActivityHashServiceTestHarness();
        var personSpec = ActivityHashServiceTestHarness.CreatePersonSpecification(
            given: "Jane",
            family: "Smith",
            gender: "female",
            birthDate: new DateOnly(2004, 5, 15),
            addressPostalCode: "XY9 8ZW"
        );
        var matchPerson = ActivityHashServiceTestHarness.CreateMatchPersonResult();

        var personSpecHash = harness.Service.StoreUniqueSearchIdFor(personSpec);
        var matchPersonHash = harness.Service.StoreUniqueSearchIdFor(matchPerson);

        Assert.Equal(personSpecHash, matchPersonHash);
    }
}
