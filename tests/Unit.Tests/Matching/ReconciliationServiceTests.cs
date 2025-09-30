using MatchingApi.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

namespace Unit.Tests.Matching;

public class ReconciliationServiceTests
{
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new(MockBehavior.Loose);
    private readonly Mock<IAuditLogger> _auditLogger = new(MockBehavior.Loose);

    [Fact]
    public async Task NoNhsNumberShouldError()
    {
        // Arrange
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("1234567890"))
            .ReturnsAsync(new DemographicResult { Result = new NhsPerson { NhsNumber = "1234567890" } });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
        };

        // Act
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Person);
        Assert.Single(result.Errors);
        Assert.Equal("Missing Nhs Number", result.Errors[0]);
    }

    [Fact]
    public async Task MinimalDataShouldNotError()
    {
        // Arrange
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = new NhsPerson { NhsNumber = "9449305552" } });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
    public async Task InvalidNhsNumberShouldHandleError()
    {
        // Arrange
        var errorMessage = "The NHS Number was not valid";
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("1234567890"))
            .ReturnsAsync(new DemographicResult { ErrorMessage = errorMessage });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

        var request = new ReconciliationRequest
        {
            NhsNumber = "1234567890"
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
    public async Task PersonNotFoundShouldHandleError()
    {
        // Arrange
        var errorMessage = "Person not found";
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { ErrorMessage = errorMessage });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
    public async Task FullDataShouldReturnSupersededNhsNumber()
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
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
        Assert.Equal(8, result.Differences?.Count);
        Assert.Equal("NhsNumber", result.Differences?[0].FieldName);
        Assert.Equal(request.NhsNumber, result.Differences?[0].Local);
        Assert.Equal(nhsPerson.NhsNumber, result.Differences?[0].Nhs);
        Assert.Equal(ReconciliationStatus.SupersededNhsNumber, result.Status);
    }

    [Fact]
    public async Task FullDataShouldReturnManyDifferences()
    {
        // Arrange
        var nhsPerson = new NhsPerson
        {
            NhsNumber = "9449305552",
            AddressPostalCodes = ["AB12 3CD", "BC34 5EF"],
            FamilyNames = ["Smith", "Jones"],
            GivenNames = ["John", "Jane"],
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = "M",
            PhoneNumbers = ["0123456789", "+44 123456789"],
            Emails = ["john.smith@example", "jane.smith@example"],
        };
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
        Assert.Equal(7, result.Differences?.Count);
        Assert.Equal("BirthDate", result.Differences?[0].FieldName);
        Assert.Equal(request.BirthDate?.ToString("yyyy-MM-dd"), result.Differences?[0].Local);
        Assert.Equal(nhsPerson.BirthDate?.ToString("yyyy-MM-dd"), result.Differences?[0].Nhs);
        Assert.Equal(ReconciliationStatus.ManyDifferences, result.Status);
    }

    [Fact]
    public async Task FullDataShouldReturnOneDifference()
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
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
        Assert.Equal(ReconciliationStatus.OneDifference, result.Status);
    }

    [Fact]
    public async Task FullDataShouldReturnNoDifferences()
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
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
    public async Task NullDataShouldReturnNoDifferences()
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
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId("9449305552"))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        var sut = new ReconciliationService(NullLogger<MatchingService>.Instance, _nhsFhirClient.Object, _auditLogger.Object);

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
        Assert.Equal(0, result.Differences?.Count);
        Assert.Equal(ReconciliationStatus.NoDifferences, result.Status);
    }
}