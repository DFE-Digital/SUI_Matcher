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

    private const string ValidNhsNumber = "9449305552";
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
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics});

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
            .ReturnsAsync(new DemographicResult { Result = matchedDemographics});
        
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
            d => d is { FieldName: nameof(NhsPerson.NhsNumber), Local: ValidNhsNumber, Nhs: "9999999993"});       
    }
    
    [Fact(Skip = "Todo")]
    public async Task ReconcileAsync_WhenLocalNHSNumberIsSuperseded_ReturnCorrectStatusWithDifferences() {}

    [Theory(Skip = "Todo")]
    [ClassData(typeof(SuccessfulCasesTestData))]
    public async Task ReconcileAsync_WhenReconcilitationSucceeds_ReturnCorrectStatusAndDemographicsAndDifferences() {}
    
    
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
            .ReturnsAsync(new DemographicResult { Result = nhsNoAPerson});
        
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
            .ReturnsAsync(new DemographicResult { Result = nhsNoBPerson});

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
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

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
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

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
        // ARRANGE
        
        // NHS number A has been superceded by NHS number B
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

        // When fetching demographics for NHS number A, NHS number B is returned
        var nhsPerson = new NhsPerson
        {
            NhsNumber = nhsNoB,
            AddressPostalCodes = ["AB12 3CD", "BC34 5EF"],
            FamilyNames = ["Smith", "Jones"],
            GivenNames = ["John", "Jane"],
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = "M",
            PhoneNumbers = ["0123456789", "+44 123456789"],
            Emails = ["john.smith@example", "jane.smith@example"],
        };       
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(nhsNoA))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        
        // When fetching demographics for NHS number B, again NHS number B is returned
        _nhsFhirClient
            .Setup(x => x.PerformSearchByNhsId(nhsNoB))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });

        // Build reconcilitation service on these mocks
        var sut = new ReconciliationService(
            _matchingService.Object,
            NullLogger<ReconciliationService>.Instance, 
            _nhsFhirClient.Object
        );

        // ACT
        
        // Reconcilitation request comes in for NHS number A
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
        Assert.Equal("NhsNumber", result.Differences?[0].FieldName); // NHS number in differences
        Assert.Equal(nhsNoA, result.Differences?[0].Local); // NHS number A is local
        Assert.Equal(nhsNoB, result.Differences?[0].Nhs); // NHS number B is NHS
        
        // When we try and fetch demographics for NHS number A, we get NHS number B
        // Therefore NHS number A is superceded by NHS number B.
        Assert.Equal(ReconciliationStatus.LocalNhsNumberIsSuperseded, result.Status); 
    }

    [Fact]
    public async Task ReconcileAsync_WithFullDataAndMultipleMismatches_ReturnsDifferencesCorrectly()
    {
        // ARRANGE
        var request = new ReconciliationRequest
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCode = "AA11 2BB",
            Family = "",
            Given = "David",
            Gender = "Male",
            Phone = "",
            BirthDate = null,
            Email = "david.hamilton@example.com",
        };
        
        // A matched NHS number should be found for the request's demographics
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.Match,
                    NhsNumber = ValidNhsNumber,
                }
            });
       
        // Demographics should be returned for the matched NHS number, with
        // differences compared to the request's demographics
        var nhsPerson = new NhsPerson
        {
            NhsNumber = ValidNhsNumber,
            AddressPostalCodes = ["AB12 3CD", "BC34 5EF"],
            FamilyNames = ["Smith", "Jones"],
            GivenNames = [],
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = "M",
            PhoneNumbers = [],
            Emails = ["john.smith@example", "jane.smith@example"],
        };
        _nhsFhirClient.Setup(x => x.PerformSearchByNhsId(ValidNhsNumber))
            .ReturnsAsync(new DemographicResult { Result = nhsPerson });
        
        // Build service based on mocks
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

        // ACT
        // Reconcile the request
        var result = await sut.ReconcileAsync(request);

        // ASSERT
        
        // Number of differences is correct
        Assert.Equal(7, result.Differences?.Count);                     
        
        // Birthdate difference is present and values written to correctly
        var birthDateDifference = result.Differences?.SingleOrDefault(d => d.FieldName == "BirthDate");
        Assert.NotNull(birthDateDifference);                            
        Assert.Equal(request.BirthDate?.ToString("yyyy-MM-dd"), birthDateDifference.Local);
        Assert.Equal(nhsPerson.BirthDate?.ToString("yyyy-MM-dd"), birthDateDifference.Nhs);
        
        // Status is 'Differences' and differences have been correctly encoded
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
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

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
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

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
    public async Task ReconcileAsync_WithNullData_ReturnsStatusOfLocalDemographicsDidNotMatchToAnNhsNumber()
    {
        // ARRANGE
        
        // Request contains all empty demographics, but NHS number is given
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

        // No match is found for empty demographics
        _matchingService.Setup(x => x.SearchAsync(It.IsAny<SearchSpecification>(), false))
            .ReturnsAsync(new PersonMatchResponse
            {
                Result = new MatchResult
                {
                    MatchStatus = MatchStatus.NoMatch,
                    NhsNumber = "",
                }
            });

        // Build service on mocks
        var sut = new ReconciliationService(_matchingService.Object, NullLogger<ReconciliationService>.Instance, _nhsFhirClient.Object);

        // ACT
        var result = await sut.ReconcileAsync(request);

        // Assert
        Assert.Equal(ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber, result.Status);
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