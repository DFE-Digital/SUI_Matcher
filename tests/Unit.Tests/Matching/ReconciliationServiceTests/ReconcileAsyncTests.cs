using MatchingApi.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Endpoint;
using Shared.Models;

namespace Unit.Tests.Matching.ReconciliationServiceTests;

public class ReconcileAsyncTests
{
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IMatchingService> _matchingService = new(MockBehavior.Loose);

    public const string ValidNhsNumber = "9449305552";
    private const string InvalidNhsNumber = "1234567890";

    [Fact]
    public async Task ReconcileAsync_WhenLocalDemographicsDidNotMatch_ReturnCorrectStatusAndNoDifferences()
    {
        // ARRANGE

        // A request comes in with full demographics and a valid NHS number
        var request = new ReconciliationRequest
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // When matching is attempted with the request demographics, no match is returned
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.NoMatch,
                    NhsNumber = ""
                }
            });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance,
            _nhsFhirClient.Object
        );

        // ACT
        var result = await sut.ReconcileAsync(request);

        // ASSERT

        // No match status returned, with no differences or demographics returned
        Assert.Equal(ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber, result.Status);
        Assert.Empty(result.Differences);
        Assert.Null(result.Person);
    }

    [Fact]
    public async Task ReconcileAsync_WhenLocalNHSNumberIsNotValid_ReturnCorrectStatusAndNhsNumberDifference()
    {
        // ARRANGE

        // A request comes in with full demographics and an invalid NHS number
        var request = new ReconciliationRequest
        {
            NhsNumber = InvalidNhsNumber,
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // When matching is attempted with the request demographics, a match is returned to a different NHS number
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = ValidNhsNumber
                }
            });

        // Return similar demographics for the matched NHS number 
        var matchedDemographics = new NhsPerson
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCodes = ["AA11 2BB"],
            FamilyNames = ["Hamilton"],
            GivenNames = ["David"],
            BirthDate = new DateOnly(1990, 01, 02),
            Gender = "M",
            PhoneNumbers = ["123454321"],
            Emails = ["david.hamilton@example.com"],
        };
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance,
            _nhsFhirClient.Object
        );

        // ACT
        var result = await sut.ReconcileAsync(request);

        // ASSERT

        // Invalid local NHS number status returned, and the NHS number should exist in the differences
        Assert.Equal(ReconciliationStatus.LocalNhsNumberIsNotValid, result.Status);
        Assert.Single(result.Differences,
            d => d is { FieldName: nameof(NhsPerson.NhsNumber), Local: InvalidNhsNumber, Nhs: ValidNhsNumber });
    }

    [Fact]
    public async Task ReconcileAsync_WhenLocalNHSNumberIsNotFoundInNHS_ReturnCorrectStatusAndNhsNumberDifferences()
    {
        // ARRANGE

        // A request comes in with full demographics and a valid NHS number
        var request = new ReconciliationRequest
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // When matching is attempted with the request demographics, a match is returned to a different NHS number
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = "9999999993"
                }
            });

        // Return similar demographics for the matched NHS number 
        var matchedDemographics = new NhsPerson
        {
            NhsNumber = "9999999993",
            AddressPostalCodes = ["AA11 2BB"],
            FamilyNames = ["Hamilton"],
            GivenNames = ["David"],
            BirthDate = new DateOnly(1990, 01, 02),
            Gender = "M",
            PhoneNumbers = ["123454321"],
            Emails = ["david.hamilton@example.com"],
        };
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId("9999999993"))
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics });

        // Return not found error when fetching demographics for the request NHS number
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Status = Status.PatientNotFound });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance,
            _nhsFhirClient.Object
        );

        // ACT
        var result = await sut.ReconcileAsync(request);

        // ASSERT

        // Local NHS number is not found in NHS status returned, and the NHS number should exist in the differences
        Assert.Equal(ReconciliationStatus.LocalNhsNumberIsNotFoundInNhs, result.Status);
        Assert.Single(result.Differences,
            d => d is { FieldName: nameof(NhsPerson.NhsNumber), Local: ValidNhsNumber, Nhs: "9999999993" });
    }

    [Fact]
    public async Task ReconcileAsync_WhenLocalNHSNumberIsSuperseded_ReturnCorrectStatusWithDifferences()
    {
        // ARRANGE

        // A request comes in with full demographics and a valid NHS number
        var request = new ReconciliationRequest
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // When matching is attempted with the request demographics, a match is returned to a different NHS number
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = "9999999993"
                }
            });

        // Return similar demographics for the matched NHS number 
        var matchedDemographics = new NhsPerson
        {
            NhsNumber = "9999999993",
            AddressPostalCodes = ["AA11 2BB"],
            FamilyNames = ["Hamilton"],
            GivenNames = ["David"],
            BirthDate = new DateOnly(1990, 01, 02),
            Gender = "M",
            PhoneNumbers = ["123454321"],
            Emails = ["david.hamilton@example.com"],
        };
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId("9999999993"))
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics });

        // Return the same demographics and the matched NHS number, when querying demographics for the request NHS number 
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance,
            _nhsFhirClient.Object
        );

        // ACT
        var result = await sut.ReconcileAsync(request);

        // ASSERT

        // Superseded status returned, and the NHS number should exist in the differences
        Assert.Equal(ReconciliationStatus.LocalNhsNumberIsSuperseded, result.Status);
        Assert.Single(result.Differences,
            d => d is { FieldName: nameof(NhsPerson.NhsNumber), Local: ValidNhsNumber, Nhs: "9999999993" });
    }

    [Theory]
    [ClassData(typeof(SuccessfulCasesTestData))]
    public async Task ReconcileAsync_WhenReconcilitationSucceeds_ReturnCorrectStatusAndDemographicsAndDifferences(SuccessfulCase successfulCase)
    {
        // ARRANGE

        // A matched NHS number should be found for the request's demographics
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = successfulCase.MatchedNhsNumber,
                }
            });

        // Demographics should be returned for the matched NHS number and request NHS number
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(successfulCase.MatchedNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = successfulCase.NhsDemographicsForMatchedNhsNumber });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(successfulCase.Request.NhsNumber))
            .ReturnsAsync(new DemographicResult { Result = successfulCase.NhsDemographicsForRequestNhsNumber });

        // Build service based on mocks
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

        // ACT
        // Reconcile the request
        var result = await sut.ReconcileAsync(successfulCase.Request);

        // ASSERT
        Assert.Equal(successfulCase.ExpectedStatus, result.Status);
        Assert.Equivalent(successfulCase.ExpectedDifferences, result.Differences);
    }


    [Fact]
    public async Task ReconcileAsync_WhenRequestAndMatchNHSNumberDifferButNotSuperceded_ShouldGiveDemographicsForMatchedNumber()
    {
        // ARRANGE

        // NHS number A is given in the reconciliation request, but matching the
        // demographics returns NHS number B with no indication of a superceded
        // NHS number.
        var nhsNoA = ValidNhsNumber;
        var nhsNoB = "3456789012";

        // When matching is attempted with the request demographics, NHS number B is returned
        _matchingService
            .Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = nhsNoB
                }
            });

        // When fetching demographics for NHS number A, return NHS number A with
        // its demographics
        var nhsNoAPerson = new NhsPerson
        {
            NhsNumber = nhsNoA,
            AddressPostalCodes = ["DE1 8JQ"],
            FamilyNames = ["Norris"],
            GivenNames = ["Darlene"],
            BirthDate = new DateOnly(2000, 12, 25),
            Gender = "F",
            PhoneNumbers = ["+43 1245 654346"],
            Emails = ["darlene.norris@hotmail.com"],
        };
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(nhsNoA))
            .ReturnsAsync(new DemographicResult { Result = nhsNoAPerson });

        // When fetching demographics for NHS number B, return NHS number B with
        // demographics closer to the request's demographics.
        var nhsNoBPerson = new NhsPerson
        {
            NhsNumber = nhsNoB,
            AddressPostalCodes = ["AA11 2BB"],
            FamilyNames = ["Hamilton"],
            GivenNames = ["David"],
            BirthDate = new DateOnly(1990, 01, 02),
            Gender = "M",
            PhoneNumbers = ["123454321"],
            Emails = ["david.hamilton@example.com"],
        };
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(nhsNoB))
            .ReturnsAsync(new DemographicResult { Result = nhsNoBPerson });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance,
            _nhsFhirClient.Object
        );

        // ACT

        // Reconcilitation request comes in for NHS number A, with NHS number B's demographics
        var result = await sut.ReconcileAsync(new ReconciliationRequest
        {
            NhsNumber = nhsNoA,
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        });

        // ASSERT
        var nhsNumberDifference = result.Differences.SingleOrDefault(a => a.FieldName == "NhsNumber");
        Assert.NotNull(nhsNumberDifference);             // NHS number in difference
        Assert.Equal(nhsNoA, nhsNumberDifference.Local); // NHS number A is local
        Assert.Equal(nhsNoB, nhsNumberDifference.Nhs);   // NHS number B is NHS

        // Since we can individually fetch demographics for both NHS numbers,
        // the NHS number has not been superceded.
        Assert.NotEqual(ReconciliationStatus.LocalNhsNumberIsSuperseded, result.Status);

        // The demographics returned should be from NHS number B
        Assert.Equal(result.Person, nhsNoBPerson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldLogCompleted_WhenInvalidNhsNumber()
    {
        // Arrange
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match
                }
            });
        var logger = Mock.Of<ILogger<ReconciliationService>>();
        var sut = new ReconciliationService(_matchingService.Object, logger, _nhsFhirClient.Object);

        // Act
        await sut.ReconcileAsync(new ReconciliationRequest() { NhsNumber = InvalidNhsNumber });

        // Assert
        Mock.Get(logger).Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[RECONCILIATION_COMPLETED]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldLogCompleted_WhenNhsNumberValid()
    {
        // Arrange
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = new NhsPerson { NhsNumber = ValidNhsNumber } });
        var logger = Mock.Of<ILogger<ReconciliationService>>();
        var sut = new ReconciliationService(_matchingService.Object, logger, _nhsFhirClient.Object);

        // Act
        await sut.ReconcileAsync(new ReconciliationRequest() { NhsNumber = ValidNhsNumber });

        // Assert
        Mock.Get(logger).Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[RECONCILIATION_COMPLETED]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}