using MatchingApi.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

namespace Unit.Tests.Matching.ReconciliationServiceTests;

public class ReconcileAsyncTests
{
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IAuditLogger> _auditLogger = new(MockBehavior.Loose);
    private readonly Mock<IMatchingService> _matchingService = new(MockBehavior.Loose);

    private const string ValidNhsNumber = "9449305552";
    private const string InvalidNhsNumber = "1234567890";

    [Fact]
    public async Task ReconcileAsync_WhenNhsNumberIsMissing_ReturnsError()
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
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        // Act
        var result = await sut.ReconcileAsync(new ReconciliationRequest());

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Person);
        Assert.Single(result.Errors);
        Assert.Equal("Missing Nhs Number", result.Errors[0]);
    }

    [Fact]
    public async Task ReconcileAsync_WhenReconciliationRequestNhsNumberIsEmpty_ShouldUseMatchingServiceNhsNumber()
    {
        // Arrange
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = ValidNhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = new NhsPerson { NhsNumber = ValidNhsNumber } });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = ""
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(ValidNhsNumber, result.Person.NhsNumber);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ReconcileAsync_WithMinimalData_ShouldNotError()
    {
        // Arrange
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = "9449305552",
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = new NhsPerson { NhsNumber = "9449305552" } });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552"
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ReconcileAsync_WhenNhsNumberIsInvalid_ReturnsError()
    {
        // Arrange
        const string errorMessage = "The NHS Number was not valid";
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = InvalidNhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(InvalidNhsNumber))
            .ReturnsAsync(new DemographicResult { ErrorMessage = errorMessage });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = InvalidNhsNumber
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Person);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Errors.First());
    }

    [Fact]
    public async Task ReconcileAsync_WhenPersonNotFound_ReturnsError()
    {
        // Arrange
        const string errorMessage = "Person not found";
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = "9449305552",
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { ErrorMessage = errorMessage });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552"
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Person);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Errors[0]);
    }

    [Fact]
    public async Task ReconcileAsync_WhenNhsNumberIsSuperseded_ReturnsSupersededStatus()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9999999999",
            AddressPostalCodes = ["AB12 3CD", "BC34 5EF"],
            FamilyNames = ["Smith", "Jones"],
            GivenNames = ["John", "Jane"],
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = "M",
            PhoneNumbers = ["0123456789", "+44 123456789"],
            Emails = ["john.smith@example", "jane.smith@example"],
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = nhsPerson.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552",
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(9, result.Differences?.Count);
        Assert.Equal("NhsNumber", result.Differences?[0].FieldName);
        Assert.Equal(request.NhsNumber, result.Differences?[0].Local);
        Assert.Equal(nhsPerson.NhsNumber, result.Differences?[0].Nhs);
        Assert.Equal(ReconciliationStatus.SupersededNhsNumber, result.Status);
    }

    [Fact]
    public async Task ReconcileAsync_WithFullDataAndMultipleMismatches_ReturnsDifferences()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9449305552",
            AddressPostalCodes = ["AB12 3CD", "BC34 5EF"],
            FamilyNames = ["Smith", "Jones"],
            GivenNames = [],
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = "M",
            PhoneNumbers = [],
            Emails = ["john.smith@example", "jane.smith@example"],
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = nhsPerson.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552",
            AddressPostalCode = "AA11 2BB",
            Family = "",
            Given = "David",
            Gender = "Male",
            Phone = "",
            BirthDate = null,
            Email = "david.hamilton@example.com",
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(7, result.Differences?.Count);
        Assert.Equal("BirthDate", result.Differences?[0].FieldName);
        Assert.Equal(request.BirthDate?.ToString("yyyy-MM-dd"), result.Differences?[0].Local);
        Assert.Equal(nhsPerson.BirthDate?.ToString("yyyy-MM-dd"), result.Differences?[0].Nhs);
        Assert.Equal(ReconciliationStatus.Differences, result.Status);
        Assert.Equal("BirthDate:LA - Gender - Given:NHS - Family:LA - Email - Phone:Both - AddressPostalCode", result.DifferenceString);
    }

    [Fact]
    public async Task ReconcileAsync_WithOneMismatch_ReturnsDifference()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9449305552",
            AddressPostalCodes = ["aA11 2BB", "BC34 5EF"],
            FamilyNames = ["hamilton", "Jones"],
            GivenNames = ["david", "Jane"],
            BirthDate = new DateOnly(1990, 1, 2),
            Gender = "male",
            PhoneNumbers = ["123454321", "+44 123456789"],
            Emails = ["david.hamilton@example.com", "jane.smith@example"],
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = nhsPerson.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552",
            AddressPostalCode = "BB22 9ZZ",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(1, result.Differences?.Count);
        Assert.Equal(ReconciliationStatus.Differences, result.Status);
    }

    [Fact]
    public async Task ReconcileAsync_WithMatchingData_ReturnsNoDifferences()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9449305552",
            AddressPostalCodes = ["aA11 2BB", "BC34 5EF"],
            FamilyNames = ["hamilton", "Jones"],
            GivenNames = ["david", "Jane"],
            BirthDate = new DateOnly(1990, 1, 2),
            Gender = "male",
            PhoneNumbers = ["123454321", "+44 123456789"],
            Emails = ["david.hamilton@example.com", "jane.smith@example"],
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = nhsPerson.NhsNumber,
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552",
            AddressPostalCode = "AA11 2BB",
            Family = "Hamilton",
            Given = "David",
            Gender = "Male",
            Phone = "123454321",
            BirthDate = new DateOnly(1990, 01, 02),
            Email = "david.hamilton@example.com",
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(0, result.Differences?.Count);
        Assert.Equal(ReconciliationStatus.NoDifferences, result.Status);
    }

    [Fact]
    public async Task ReconcileAsync_WithNullData_ReturnsAllFieldsAsDifferences()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9449305552",
            AddressPostalCodes = [],
            FamilyNames = [],
            GivenNames = [],
            BirthDate = null,
            Gender = null,
            PhoneNumbers = [],
            Emails = [],
        };
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.NoMatch,
                    NhsNumber = "",
                }
            });
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "9449305552",
            AddressPostalCode = null,
            Family = null,
            Given = null,
            Gender = null,
            Phone = null,
            BirthDate = null,
            Email = null,
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Person);
        Assert.Equal(8, result.Differences?.Count);
        Assert.Equal(ReconciliationStatus.Differences, result.Status);
        Assert.Equal("BirthDate:Both - Gender:Both - Given:Both - Family:Both - Email:Both - Phone:Both - AddressPostalCode:Both - MatchingNhsNumber:NHS", result.DifferenceString);
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
        var sut = new ReconciliationService(_matchingService.Object, logger, _nhsFhirClient.Object, _auditLogger.Object);

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
        var sut = new ReconciliationService(_matchingService.Object, logger, _nhsFhirClient.Object, _auditLogger.Object);

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