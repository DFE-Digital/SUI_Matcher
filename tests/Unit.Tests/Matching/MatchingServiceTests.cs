using System.Diagnostics;

using MatchingApi.Search;
using MatchingApi.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json;

using Shared;
using Shared.Endpoint;
using Shared.Logging;
using Shared.Models;

using Unit.Tests.Util;

namespace Unit.Tests.Matching;

public sealed class MatchingServiceTests
{
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<ILogger<MatchingService>> _loggerMock = new();
    private readonly Mock<INhsFhirClient> _nhsFhirClient = new();
    private readonly ValidationService _validationService = new();
    private MatchingService _sut;

    public MatchingServiceTests()
    {
        _sut = new MatchingService(_loggerMock.Object, _nhsFhirClient.Object, _validationService, _auditLogger.Object);
    }

    [Fact]
    public async Task ShouldLogOptionalProperties_WhenProvidedInPersonSpecification()
    {
        // Arrange
        SearchSpecification personSpecification = new()
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
            OptionalProperties = new Dictionary<string, object>
            {
                { "CustomProperty1", "Value1" },
                { "CustomProperty2", "Value2" }
            }
        };

        // Act 
        await _sut.SearchAsync(personSpecification);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CustomProperty1") && v.ToString()!.Contains("Value1") &&
                                          v.ToString()!.Contains("CustomProperty2") && v.ToString()!.Contains("Value2")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmptyPersonModelReturnsError()
    {
        var result = await _sut.SearchAsync(new SearchSpecification());

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
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            });

        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.ManyMatch, result.Result!.MatchStatus);
    }

    [Theory]
    [InlineData("2000-11-16", 5)] // non-swappable day/month in dob - so expect 5 search strategies
    [InlineData("2000-11-10", 6)] // swappable day/month in dob - so expect 6 search strategies
    public async Task MultipleQueryStrategiesWereUsed(string dob, int expectedSearchStrategiesUsed)
    {
        var dateOfBirth = DateOnly.Parse(dob);



        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = dateOfBirth,
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        _nhsFhirClient.Verify(x => x.PerformSearch(It.IsAny<SearchQuery>()), Times.Exactly(expectedSearchStrategiesUsed));
    }

    [Fact]
    public async Task NoMatch()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Unmatched
            });




        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        Assert.Null(result.Result.ProcessStage);
    }

    [Fact]
    public async Task SingleCandidateMatch()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.94m
            });




        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.PotentialMatch, result.Result!.MatchStatus);
        Assert.Equal(0.94m, result.Result.Score);
    }

    [Fact]
    public async Task SingleConfirmedMatch()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });




        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.99m, result.Result.Score);
    }

    [Fact]
    public async Task SingleQuotesInGivenAndFamilyAreEscaped()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given!.Contains("O'Connor") && q.Family!.Contains("D'Angelo"))))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.95m
            });




        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "D'Angelo",
            Given = "O'Connor",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.95m, result.Result.Score);

        // Verify that PerformSearch was called with the correct values
        _nhsFhirClient.Verify(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            q.Given!.Contains("O'Connor") && q.Family!.Contains("D'Angelo"))));
    }

    [Fact]
    public async Task ShouldLogPersonSpecificationAndResultStatus_WithMatchCompletedForAggregate()
    {
        // Arrange
        var eighteenYearsAgo = DateTime.UtcNow.AddYears(-18);

        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(eighteenYearsAgo.Year, eighteenYearsAgo.Month, eighteenYearsAgo.Day),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        await _sut.SearchAsync(model);

        // Assert
        _loggerMock.Verify(logger => logger.Log(
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
            builder.AddConsole(options => options.FormatterName = SharedConstants.LogFormatter)
                .AddConsoleFormatter<TestLogConsoleFormatter, TestConsoleFormatterOptions>(options =>
                {
                    options.TestLogMessages = logMessages;
                });
        });

        var logger = loggerFactory.CreateLogger<MatchingService>();
        _sut = new MatchingService(logger, _nhsFhirClient.Object, _validationService, _auditLogger.Object);

        var model = new SearchSpecification
        {
            BirthDate = new DateOnly(1970, 1, 1),
            Family = "Smith",
            Given = "John",
        };
        var searchFactory = SearchStrategyFactory.Get(SharedConstants.SearchStrategy.Strategies.Strategy1);
        var algoId = searchFactory.GetAlgorithmVersion();

        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 1
            });

        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        await _sut.SearchAsync(model);

        // Assert
        Assert.NotEmpty(logMessages);
        Assert.Contains(logMessages, x => x.Contains($"[Algorithm=v{algoId}]"));
    }

    [Fact]
    public async Task MultipleMatchesNoLogic()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.MultiMatched
            });

        var model = new PersonSpecificationForNoLogic
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(2000, 11, 11),
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.ManyMatch, result.Result!.MatchStatus);
    }

    [Fact]
    public async Task NoMatchNoLogic()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Unmatched
            });

        var model = new PersonSpecificationForNoLogic
        {
            AddressPostalCode = "TQ12 5HH",
            RawBirthDate = ["eq2000-11-11"],
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.NoMatch, result.Result!.MatchStatus);
        Assert.Equal(String.Empty, result.Result.ProcessStage);
    }

    [Fact]
    public async Task SingleConfirmedMatchNoLogic()
    {
        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        var model = new PersonSpecificationForNoLogic
        {
            AddressPostalCode = "TQ12 5HH",
            RawBirthDate = ["eq2000-11-11"],
            Email = "test@test.com",
            Family = "Smith",
            Given = "John",
            Gender = "male",
            Phone = "000000000",
        };

        var result = await _sut.SearchNoLogicAsync(model);

        Assert.NotNull(result);
        Assert.Equal(MatchStatus.Match, result.Result!.MatchStatus);
        Assert.Equal(0.99m, result.Result.Score);
    }

    [Fact]
    public async Task ShouldPickSecondStrategyIfSpecifiedInSearchSpecification()
    {
        // Arrange
        var eighteenYearsAgo = DateTime.UtcNow.AddYears(-10);

        var model = new SearchSpecification
        {
            AddressPostalCode = "TQ12 5HH",
            BirthDate = new DateOnly(eighteenYearsAgo.Year, eighteenYearsAgo.Month, eighteenYearsAgo.Day),
            Family = "Smith",
            Given = "John",
            SearchStrategy = SharedConstants.SearchStrategy.Strategies.Strategy2
        };

        _nhsFhirClient.Setup(x => x.PerformSearch(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new SearchResult
            {
                Type = SearchResult.ResultType.Matched,
                Score = 0.99m
            });

        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        await _sut.SearchAsync(model);

        // Assert
        _nhsFhirClient.Verify(x => x.PerformSearch(It.Is<SearchQuery>(q =>
            (q.FuzzyMatch == null || q.FuzzyMatch == false) && q.ExactMatch == false)));

    }
}