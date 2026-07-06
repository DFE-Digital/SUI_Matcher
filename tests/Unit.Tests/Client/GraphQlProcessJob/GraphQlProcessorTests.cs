using Eclipse.GraphQL;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using StrawberryShake;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.GraphQLProcessJob;
using SUI.Client.GraphQLProcessJob.Infrastructure;

namespace Unit.Tests.Client.GraphQlProcessJob;

public class GraphQlProcessorTests
{
    private readonly Mock<IEclipseClient> _eclipseClientMock;
    private readonly Mock<IPersonByCriteriaQuery> _personByCriteriaQueryMock;
    private readonly Mock<ILogger<GraphQlProcessor>> _loggerMock;
    private readonly Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>> _matchPersonRecordOrchestratorMock;
    private readonly IOptions<CsvMatchDataOptions> _csvMatchOptions;

    public GraphQlProcessorTests()
    {
        _eclipseClientMock = new Mock<IEclipseClient>();
        _personByCriteriaQueryMock = new Mock<IPersonByCriteriaQuery>();
        _loggerMock = new Mock<ILogger<GraphQlProcessor>>();
        _matchPersonRecordOrchestratorMock = new Mock<IMatchPersonRecordOrchestrator<CsvRecordDto>>();

        _eclipseClientMock.Setup(x => x.PersonByCriteria).Returns(_personByCriteriaQueryMock.Object);
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        _csvMatchOptions = Options.Create(new CsvMatchDataOptions
        {
            DateFormat = "dd/MM/yyyy",
            ColumnMappings = new CsvMatchDataOptions.Headers
            {
                Id = "SourceID",
                Given = "Forename",
                Family = "Surname",
                BirthDate = "DOB",
                Postcode = "PostCode",
                NhsNumber = "NHSNumber",
                Gender = "Gender"
            }
        });
    }

    [Fact]
    public async Task RunAsync_ShouldProcessSinglePage_WhenNoMoreResultsExist()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });
        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            _csvMatchOptions
        );

        // Person 1 Setup
        var dobMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_DateOfBirth>();
        dobMock.Setup(d => d.Lower).Returns(new DateOnly(1990, 5, 20));

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-123");
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns(dobMock.Object);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        // Cursor Setup
        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        // Result Struct Setup
        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock.Object);

        IEnumerable<CsvRecordDto>? capturedRecords = null;
        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CsvRecordDto>, string, CancellationToken>((records, _, _) =>
                capturedRecords = records.ToList())
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>>());

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        _personByCriteriaQueryMock.Verify(
            q => q.ExecuteAsync(25, It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()), Times.Once);
        _matchPersonRecordOrchestratorMock.Verify(
            o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), "graphql_extract",
                It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.Equal("person-123", record.Record["SourceID"]);
        Assert.Equal("John", record.Record["Forename"]);
        Assert.Equal("Doe", record.Record["Surname"]);
        Assert.Equal("20/05/1990", record.Record["DOB"]);
    }

    [Fact]
    public async Task RunAsync_ShouldProcessMultiplePages_WhenMoreResultsExist()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });
        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            _csvMatchOptions
        );

        // Page 1 Setup (John Doe)
        var dobMock1 = new Mock<IPersonByCriteria_PersonByCriteria_Results_DateOfBirth>();
        dobMock1.Setup(d => d.Lower).Returns(new DateOnly(1990, 5, 20));

        var personMock1 = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock1.Setup(p => p.Id).Returns("john-123");
        personMock1.Setup(p => p.Forename).Returns("John");
        personMock1.Setup(p => p.Surname).Returns("Doe");
        personMock1.Setup(p => p.DateOfBirth).Returns(dobMock1.Object);
        personMock1.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());

        var resultsList1 = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock1.Object };

        var cursorMock1 = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock1.Setup(c => c.Offset).Returns(0);
        cursorMock1.Setup(c => c.Returned).Returns(1);
        cursorMock1.Setup(c => c.TotalSize).Returns(2); // Total 2 results, indicating more exist

        var personByCriteriaMock1 = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock1.Setup(p => p.Results).Returns(resultsList1.AsReadOnly());
        personByCriteriaMock1.Setup(p => p.Cursor).Returns(cursorMock1.Object);

        var operationResultDataMock1 = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock1.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock1.Object);

        var operationResultMock1 = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock1.Setup(r => r.Data).Returns(operationResultDataMock1.Object);
        operationResultMock1.Setup(r => r.Errors).Returns(new List<IClientError>());

        // Page 2 Setup (Jane Smith)
        var dobMock2 = new Mock<IPersonByCriteria_PersonByCriteria_Results_DateOfBirth>();
        dobMock2.Setup(d => d.Lower).Returns(new DateOnly(1995, 8, 15));

        var personMock2 = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock2.Setup(p => p.Id).Returns("jane-456");
        personMock2.Setup(p => p.Forename).Returns("Jane");
        personMock2.Setup(p => p.Surname).Returns("Smith");
        personMock2.Setup(p => p.DateOfBirth).Returns(dobMock2.Object);
        personMock2.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());

        var resultsList2 = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock2.Object };

        var cursorMock2 = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock2.Setup(c => c.Offset).Returns(1);
        cursorMock2.Setup(c => c.Returned).Returns(1);
        cursorMock2.Setup(c => c.TotalSize).Returns(2); // No more results after this page

        var personByCriteriaMock2 = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock2.Setup(p => p.Results).Returns(resultsList2.AsReadOnly());
        personByCriteriaMock2.Setup(p => p.Cursor).Returns(cursorMock2.Object);

        var operationResultDataMock2 = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock2.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock2.Object);

        var operationResultMock2 = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock2.Setup(r => r.Data).Returns(operationResultDataMock2.Object);
        operationResultMock2.Setup(r => r.Errors).Returns(new List<IClientError>());

        // Setup query mock to return Page 1 first, then Page 2
        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock1.Object);

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 2 && r.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock2.Object);

        IEnumerable<CsvRecordDto>? capturedRecords = null;
        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CsvRecordDto>, string, CancellationToken>((records, _, _) =>
                capturedRecords = records.ToList())
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>>());

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        _personByCriteriaQueryMock.Verify(
            q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1), It.IsAny<CancellationToken>()),
            Times.Once);
        _personByCriteriaQueryMock.Verify(
            q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 2), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(capturedRecords);
        var recordsList = capturedRecords.ToList();
        Assert.Equal(2, recordsList.Count);

        Assert.Equal("john-123", recordsList[0].Record["SourceID"]);
        Assert.Equal("John", recordsList[0].Record["Forename"]);
        Assert.Equal("Doe", recordsList[0].Record["Surname"]);
        Assert.Equal("20/05/1990", recordsList[0].Record["DOB"]);

        Assert.Equal("jane-456", recordsList[1].Record["SourceID"]);
        Assert.Equal("Jane", recordsList[1].Record["Forename"]);
        Assert.Equal("Smith", recordsList[1].Record["Surname"]);
        Assert.Equal("15/08/1995", recordsList[1].Record["DOB"]);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipResult_WhenResultIsNotPerson()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });
        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            _csvMatchOptions
        );

        // Create a generic results mock that is NOT a Person (e.g. RedactedResult)
        var redactedResultMock = new Mock<IPersonByCriteria_PersonByCriteria_Results>();

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { redactedResultMock.Object };

        // Cursor Setup
        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        // Result Struct Setup
        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock.Object);

        IEnumerable<CsvRecordDto>? capturedRecords = null;
        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CsvRecordDto>, string, CancellationToken>((records, _, _) =>
                capturedRecords = records.ToList())
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>>());

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        _personByCriteriaQueryMock.Verify(
            q => q.ExecuteAsync(25, It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify that we did NOT pass any records to the orchestrator (since the redacted result was skipped)
        Assert.NotNull(capturedRecords);
        Assert.Empty(capturedRecords);
    }
}