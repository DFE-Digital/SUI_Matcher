using Hl7.Fhir.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SUI.Core.Endpoints.AuthToken;
using Newtonsoft.Json;
using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;
using SUI.Core.Services;
using SUI.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SUI.Test.Unit;

[TestClass]
public sealed class MatchingServiceTests
{
    [TestMethod]
    public async Task EmptyPersonModelReturnsError()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var result = await subj.SearchAsync(new PersonSpecification());

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.Error, result.Result!.MatchStatus);
        Assert.AreEqual(JsonConvert.SerializeObject(new DataQualityResult
        {
            Given = QualityType.NotProvided,
            Family = QualityType.NotProvided,
            Birthdate = QualityType.NotProvided,
            Gender = QualityType.NotProvided,
            Phone = QualityType.NotProvided,
            Email = QualityType.NotProvided,
            AddressPostalCode = QualityType.NotProvided
        }), JsonConvert.SerializeObject(result.DataQuality));
    }

    [TestMethod]
    public async Task MultipleMatches()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.ManyMatch, result.Result!.MatchStatus);
    }

    [TestMethod]
    [DataRow("2000-11-16", 3)] // non-swappable day/month in dob - so expect 5 search strategies
    [DataRow("2000-11-10", 4)] // swappable day/month in dob - so expect 6 search strategies
    public async Task MultpleQueryStrategiesWereUsed(string dob, int expectedSearchStrategiesUsed)
    {
        var dateOfBirth = DateOnly.Parse(dob);
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = dateOfBirth,
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.NoMatch, result.Result!.MatchStatus);
        nhsFhir.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.Exactly(expectedSearchStrategiesUsed));
    }

    [TestMethod]
    public async Task NoMatch()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Unmatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.NoMatch, result.Result!.MatchStatus);
    }

    [TestMethod]
    public async Task SingleCandidateMatch()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.94m
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.PotentialMatch, result.Result!.MatchStatus);
        Assert.AreEqual(0.94m, result.Result.Score);
    }

    [TestMethod]
    public async Task SingleConfirmedMatch()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.AreEqual(0.99m, result.Result.Score);
    }

    [TestMethod]
    public async Task SingleQuotesInGivenAndFamilyAreEscaped()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given.Contains("O'Connor") && q.Family.Contains("D'Angelo"))))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.95m
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "D'Angelo",
            Given = "O'Connor",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await subj.SearchAsync(model);

        Assert.IsNotNull(result);
        Assert.AreEqual(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.AreEqual(0.95m, result.Result.Score);

        // Verify that PerformSearch was called with the correct values
        nhsFhir.Verify(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given.Contains("O'Connor") && q.Family.Contains("D'Angelo"))));
    }
    

    private ILogger<MatchingService> CreateLogger() =>
         new Logger<MatchingService>(new LoggerFactory());
}