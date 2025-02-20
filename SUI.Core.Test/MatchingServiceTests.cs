using Json.More;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;
using SUI.Core.Services;

namespace SUI.Core.Test;

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
        Assert.AreEqual(JsonConvert.SerializeObject(new PersonMatchResponse.DataQualityResult
        {
            Given = PersonMatchResponse.QualityType.NotProvided,
            Family = PersonMatchResponse.QualityType.NotProvided,
            Birthdate = PersonMatchResponse.QualityType.Valid,
            Gender = PersonMatchResponse.QualityType.NotProvided,
            Phone = PersonMatchResponse.QualityType.NotProvided,
            Email = PersonMatchResponse.QualityType.NotProvided,
            AddressPostalCode = PersonMatchResponse.QualityType.NotProvided
        }), JsonConvert.SerializeObject(result.DataQuality));
    }

    [TestMethod]
    [DataRow("2000-11-16", 3)] // non-swappable day/month in dob - so expect 5 search strategies
    [DataRow("2000-11-10", 4)] // swappable day/month in dob - so expect 6 search strategies
    public async Task MultpleQueryStrategiesWereUsed(string dob, int expectedSearchStrategiesUsed)
    {
        var dateOfBirth = DateTime.Parse(dob);
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
    public async Task SingleConfirmedMatch()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x=>x.PerformSearch(It.IsAny<SearchQuery>()))
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
            BirthDate = new DateTime(2000,11,11),
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
            BirthDate = new DateTime(2000, 11, 11),
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
            BirthDate = new DateTime(2000, 11, 11),
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
            BirthDate = new DateTime(2000, 11, 11),
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

    private ILogger<MatchingService> CreateLogger() =>
         new Logger<MatchingService>(new LoggerFactory());
}
