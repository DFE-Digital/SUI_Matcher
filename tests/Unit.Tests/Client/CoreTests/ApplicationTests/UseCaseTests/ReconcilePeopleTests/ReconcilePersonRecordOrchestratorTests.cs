using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Models;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Application.UseCases.ReconcilePeople;

namespace Unit.Tests.Client.CoreTests.ApplicationTests.UseCaseTests.ReconcilePeopleTests;

public class ReconcilePersonRecordOrchestratorTests
{
    [Fact]
    public async Task Should_ReconcileParsedRecordAndRetainDerivedInputs()
    {
        var apiClient = new Mock<IMatchingApiClient>();
        var personParser = new Mock<IPersonSpecParser<string>>();
        var reconciliationParser = new Mock<IReconciliationDataParser<string>>();
        var logger = new Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>>();
        personParser
            .Setup(parser => parser.Parse("record"))
            .Returns(
                new PersonSpecification
                {
                    Given = "Jane",
                    Family = "Doe",
                    BirthDate = new DateOnly(2012, 5, 10),
                    RawBirthDate = ["2012-05-10"],
                    AddressPostalCode = "AA1 1AA",
                    OptionalProperties = new Dictionary<string, object>
                    {
                        ["CustomField1"] = "CustomValue1",
                        ["CustomField2"] = "CustomValue2",
                    },
                }
            );
        reconciliationParser
            .Setup(parser => parser.Parse("record"))
            .Returns(
                new ReconciliationSourceData("9999999993", "10 Example Road, Exampletown, AA1 1AA")
            );
        apiClient
            .Setup(client =>
                client.ReconcilePersonAsync(
                    It.IsAny<ReconciliationRequest>(),
                    CancellationToken.None
                )
            )
            .ReturnsAsync(
                new ReconciliationResponse
                {
                    Status = ReconciliationStatus.NoDifferences,
                    SearchId = "search-id",
                    MatchingResult = new MatchResult
                    {
                        MatchStatus = MatchStatus.Match,
                        NhsNumber = "9999999993",
                        Score = 0.96m,
                    },
                    Person = new NhsPerson
                    {
                        NhsNumber = "9999999993",
                        AddressPostalCodes = ["AA1 1AA"],
                        AddressHistory = ["current~10 Example Road~Exampletown~AA1 1AA"],
                    },
                }
            );
        var sut = new ReconcilePersonRecordOrchestrator<string>(
            logger.Object,
            apiClient.Object,
            personParser.Object,
            reconciliationParser.Object,
            new AddressComparisonOrchestrator(new SemicolonCommaNewestFirstAddressHistoryParser()),
            Options.Create(
                new PersonMatchingOptions
                {
                    SearchStrategy = Shared.SharedConstants.SearchStrategy.Strategies.Strategy4,
                    StrategyVersion = 2,
                }
            ),
            Options.Create(new OptionalPropertiesLog { Fields = ["customfield1"] })
        );

        var result = Assert.Single(
            await sut.ProcessAsync(["record"], "input.csv", CancellationToken.None)
        );

        apiClient.Verify(
            client =>
                client.ReconcilePersonAsync(
                    It.Is<ReconciliationRequest>(request =>
                        request.NhsNumber == "9999999993"
                        && request.SearchStrategy
                            == Shared.SharedConstants.SearchStrategy.Strategies.Strategy4
                        && request.OptionalProperties.Count == 0
                    ),
                    CancellationToken.None
                ),
            Times.Once
        );
        Assert.True(result.IsSuccess);
        Assert.Equal("search-id", result.ApiResult!.SearchId);
        Assert.Equal(new DateOnly(2012, 5, 10), result.SourceBirthDate);
        Assert.Equal("9999999993", result.SourceNhsNumber);
        Assert.Equal(
            AddressComparisonResult.AddressMatchStatus.Matched,
            result.AddressComparisonResults!.PrimaryAddressSame.Status
        );
        VerifyOptionalPropertiesLogged(logger);
        VerifyOptionalPropertiesScope(logger);
        VerifyAddressComparisonLogged(logger);
    }

    [Fact]
    public async Task Should_LogNoOptionalProperties_When_NoOptionalFieldsAreLoggable()
    {
        var apiClient = new Mock<IMatchingApiClient>();
        var personParser = new Mock<IPersonSpecParser<string>>();
        var reconciliationParser = new Mock<IReconciliationDataParser<string>>();
        var logger = new Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>>();
        personParser
            .Setup(parser => parser.Parse("record"))
            .Returns(
                new PersonSpecification
                {
                    Given = "Jane",
                    Family = "Doe",
                    BirthDate = new DateOnly(2012, 5, 10),
                    RawBirthDate = ["2012-05-10"],
                    AddressPostalCode = "AA1 1AA",
                    OptionalProperties = new Dictionary<string, object>
                    {
                        ["CustomField2"] = "CustomValue2",
                    },
                }
            );
        reconciliationParser
            .Setup(parser => parser.Parse("record"))
            .Returns(new ReconciliationSourceData("9999999993", null));
        apiClient
            .Setup(client =>
                client.ReconcilePersonAsync(
                    It.IsAny<ReconciliationRequest>(),
                    CancellationToken.None
                )
            )
            .ReturnsAsync(
                new ReconciliationResponse
                {
                    Status = ReconciliationStatus.NoDifferences,
                    SearchId = "search-id",
                    MatchingResult = new MatchResult { MatchStatus = MatchStatus.Match },
                }
            );
        var sut = new ReconcilePersonRecordOrchestrator<string>(
            logger.Object,
            apiClient.Object,
            personParser.Object,
            reconciliationParser.Object,
            new AddressComparisonOrchestrator(new SemicolonCommaNewestFirstAddressHistoryParser()),
            Options.Create(new PersonMatchingOptions()),
            Options.Create(new OptionalPropertiesLog { Fields = ["customfield1"] })
        );

        await sut.ProcessAsync(["record"], "input.csv", CancellationToken.None);

        VerifyNoOptionalPropertiesLogged(logger);
    }

    private static void VerifyOptionalPropertiesLogged(
        Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>> logger
    )
    {
        logger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.Is<EventId>(eventId => ContainsOptionalPropertiesEventId(eventId)),
                    It.Is<It.IsAnyType>((state, _) => ContainsOptionalPropertiesLogState(state)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    private static void VerifyNoOptionalPropertiesLogged(
        Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>> logger
    )
    {
        logger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.Is<EventId>(eventId => ContainsOptionalPropertiesEventId(eventId)),
                    It.Is<It.IsAnyType>((state, _) => ContainsNoOptionalPropertiesLogState(state)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    private static void VerifyOptionalPropertiesScope(
        Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>> logger
    )
    {
        logger.Verify(
            x =>
                x.BeginScope(
                    It.Is<It.IsAnyType>((state, _) => ContainsOptionalPropertiesScope(state))
                ),
            Times.Once
        );
    }

    private static void VerifyAddressComparisonLogged(
        Mock<ILogger<ReconcilePersonRecordOrchestrator<string>>> logger
    )
    {
        logger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) => ContainsAddressComparisonLog(state.ToString()!)
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    private static bool ContainsOptionalPropertiesLogState(object state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> logProperties)
        {
            return false;
        }

        var properties = logProperties.ToDictionary(
            logProperty => logProperty.Key,
            logProperty => logProperty.Value
        );

        return HasLogProperty(properties, "EventName", "RECONCILIATION_OPTIONAL_PROPERTIES")
            && HasLogProperty(properties, "SearchId", "search-id")
            && HasLogProperty(properties, "OptionalPropertiesStatus", "Present")
            && HasLogProperty(properties, "OptionalPropertiesCount", "1")
            && HasLogProperty(
                properties,
                "OptionalProperties",
                """{"CustomField1":"CustomValue1"}"""
            )
            && ContainsOptionalPropertiesMessage(state.ToString()!);
    }

    private static bool ContainsOptionalPropertiesEventId(EventId eventId)
    {
        return eventId.Id == 1001
            && string.Equals(
                eventId.Name,
                "RECONCILIATION_OPTIONAL_PROPERTIES",
                StringComparison.Ordinal
            );
    }

    private static bool ContainsNoOptionalPropertiesLogState(object state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> logProperties)
        {
            return false;
        }

        var properties = logProperties.ToDictionary(
            logProperty => logProperty.Key,
            logProperty => logProperty.Value
        );

        return HasLogProperty(properties, "EventName", "RECONCILIATION_OPTIONAL_PROPERTIES")
            && HasLogProperty(properties, "SearchId", "search-id")
            && HasLogProperty(properties, "OptionalPropertiesStatus", "None")
            && HasLogProperty(properties, "OptionalPropertiesCount", "0")
            && HasLogProperty(properties, "OptionalProperties", "No optional properties")
            && state.ToString()!.Contains("No optional properties", StringComparison.Ordinal);
    }

    private static bool ContainsOptionalPropertiesScope(object state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> scopeProperties)
        {
            return false;
        }

        var properties = scopeProperties.ToDictionary(
            scopeProperty => scopeProperty.Key,
            scopeProperty => scopeProperty.Value
        );

        return HasLogProperty(properties, "Optional_CustomField1", "CustomValue1")
            && !properties.ContainsKey("Optional_CustomField2");
    }

    private static bool ContainsOptionalPropertiesMessage(string message)
    {
        return message.Contains(
                """OptionalProperties: {"CustomField1":"CustomValue1"}""",
                StringComparison.Ordinal
            )
            && !message.Contains("CustomField2", StringComparison.Ordinal)
            && !message.Contains("CustomValue2", StringComparison.Ordinal);
    }

    private static bool HasLogProperty(
        IReadOnlyDictionary<string, object?> properties,
        string key,
        string expectedValue
    )
    {
        return properties.TryGetValue(key, out var actualValue)
            && string.Equals(actualValue?.ToString(), expectedValue, StringComparison.Ordinal);
    }

    private static bool ContainsAddressComparisonLog(string logMessage)
    {
        return logMessage.Contains("[ADDRESS_COMPARISON_COMPLETED]", StringComparison.Ordinal)
            && logMessage.Contains("SearchId: search-id", StringComparison.Ordinal)
            && logMessage.Contains("PrimaryAddressSame: Matched", StringComparison.Ordinal)
            && logMessage.Contains("AddressHistoriesIntersect: Matched", StringComparison.Ordinal)
            && logMessage.Contains(
                "PrimarySourceAddressInPDSHistory: Matched",
                StringComparison.Ordinal
            )
            && logMessage.Contains(
                "PrimaryPDSAddressInSourceHistory: Matched",
                StringComparison.Ordinal
            );
    }
}
