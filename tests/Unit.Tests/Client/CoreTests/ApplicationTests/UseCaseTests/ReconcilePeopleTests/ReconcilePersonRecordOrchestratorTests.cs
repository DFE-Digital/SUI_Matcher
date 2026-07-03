using Microsoft.Extensions.Logging.Abstractions;
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
            NullLogger<ReconcilePersonRecordOrchestrator<string>>.Instance,
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
            Options.Create(
                new OptionalPropertiesLog
                {
                    Fields = new Dictionary<string, string> { ["customfield1"] = string.Empty },
                }
            )
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
                        && request.OptionalProperties.Count == 1
                        && request.OptionalProperties["CustomField1"].Equals("CustomValue1")
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
    }
}
