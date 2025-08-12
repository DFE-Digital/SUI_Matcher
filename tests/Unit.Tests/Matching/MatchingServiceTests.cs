using System.Diagnostics;

using MatchingApi.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json;

using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

using Unit.Tests.Util;

namespace Unit.Tests.Matching;

public sealed class MatchingServiceTests
{
    private readonly Mock<IAuditLogger> _auditLogger = new();
    [Fact]
    public async Task EmptyPersonModelReturnsError()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

        var result = await subj.SearchAsync(new PersonSpecification());

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Error, result.Result!.MatchStatus);
        Assert.Equal(JsonConvert.SerializeObject(new DataQualityResult
        {
            Given = QualityType.NotProvided,
            Family = QualityType.NotProvided,
            BirthDate = QualityType.NotProvided,
            Gender = QualityType.NotProvided,
            Phone = QualityType.NotProvided,
            Email = QualityType.NotProvided,
            AddressPostalCode = QualityType.NotProvided
        }), JsonConvert.SerializeObject(result.DataQuality));
    }

    [Fact]
    public async Task MultipleMatches()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.ManyMatch, result.Result!.MatchStatus);
    }

    [Theory]
    [InlineData("2000-11-16", 5)] // non-swappable day/month in dob - so expect 5 search strategies
    [InlineData("2000-11-10", 6)] // swappable day/month in dob - so expect 6 search strategies
    public async Task MultipleQueryStrategiesWereUsed(string dob, int expectedSearchStrategiesUsed)
    {
        var dateOfBirth = DateOnly.Parse(dob);
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        nhsFhir.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.Exactly(expectedSearchStrategiesUsed));
    }

    [Fact]
    public async Task NoMatch()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Unmatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        Assert.Null(result.Result.ProcessStage);
    }

    [Fact]
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
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.PotentialMatch, result.Result!.MatchStatus);
        Assert.Equal(0.94m, result.Result.Score);
    }

    [Fact]
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
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.99m, result.Result.Score);
    }

    [Fact]
    public async Task SingleQuotesInGivenAndFamilyAreEscaped()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given!.Contains("O'Connor") && q.Family!.Contains("D'Angelo"))))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.95m
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.95m, result.Result.Score);

        // Verify that PerformSearch was called with the correct values
        nhsFhir.Verify(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given!.Contains("O'Connor") && q.Family!.Contains("D'Angelo"))));
    }

    [Fact]
    public async Task ShouldLogPersonSpecificationAndResultStatus_WithMatchCompletedForAggregate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MatchingService>>();
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(mockLogger.Object, nhsFhir.Object, validationService, _auditLogger.Object);

        var eighteenYearsAgo = DateTime.UtcNow.AddYears(-18);

        var model = new PersonSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(eighteenYearsAgo.Year, eighteenYearsAgo.Month, eighteenYearsAgo.Day),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        await subj.SearchAsync(model);

        // Assert
        mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[MATCH_COMPLETED]") &&
                                              v.ToString()!.Contains("MatchStatus: Match") &&
                                              v.ToString()!.Contains("AgeGroup: 16-18 years") &&
                                              v.ToString()!.Contains("Gender: male") &&
                                              v.ToString()!.Contains("Postcode: TQ12 5HH")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public async Task ShouldPrependCurrentAlgorithmVersionToLogMessage()
    {
        // Arrange
        var logMessages = new List<string>();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options => options.FormatterName = Shared.Constants.LogFormatter)
                .AddConsoleFormatter<TestLogConsoleFormatter, TestConsoleFormatterOptions>(options =>
                {
                    options.TestLogMessages = logMessages;
                });
        });

        var logger = loggerFactory.CreateLogger<MatchingService>();
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);
        var validationService = new ValidationService();
        var subj = new MatchingService(logger, nhsFhir.Object, validationService, _auditLogger.Object);

        var model = new PersonSpecification
        {
            BirthDate = new DateOnly(1970, 1, 1),
            Family = "Smith",
            Given = "John",
        };

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 1
            });

        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        await subj.SearchAsync(model);

        // Assert
        Assert.NotEmpty(logMessages);
        Assert.Contains(logMessages, x => x.Contains($"[Algorithm=v{MatchingService.AlgorithmVersion}]"));
    }

    [Fact]
    public async Task MultipleMatchesNoLogic()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        var result = await subj.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.ManyMatch, result.Result!.MatchStatus);
    }

    [Fact]
    public async Task NoMatchNoLogic()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Unmatched
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        var result = await subj.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        Assert.Equal(String.Empty, result.Result.ProcessStage);
    }

    [Fact]
    public async Task SingleConfirmedMatchNoLogic()
    {
        var nhsFhir = new Mock<INhsFhirClient>(MockBehavior.Loose);

        nhsFhir.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        var validationService = new ValidationService();
        var subj = new MatchingService(CreateLogger(), nhsFhir.Object, validationService, _auditLogger.Object);

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

        var result = await subj.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.99m, result.Result.Score);
    }

    private static Logger<MatchingService> CreateLogger() =>
         new Logger<MatchingService>(new LoggerFactory());
}