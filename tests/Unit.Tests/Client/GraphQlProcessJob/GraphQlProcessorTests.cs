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
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 100),
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
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock1.Object);

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 2 && r.PageSize == 100),
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
            .Setup(q => q.ExecuteAsync(25, It.Is<RequestCursorInput>(r => r.PageNumber == 1 && r.PageSize == 100),
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

    [Fact]
    public async Task RunAsync_ShouldReturnEarly_WhenCancellationTokenIsCancelled()
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

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>>());

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        _personByCriteriaQueryMock.Verify(
            q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _matchPersonRecordOrchestratorMock.Verify(
            o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldThrowException_WhenGraphQLResultHasErrors()
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

        var errorMock = new Mock<IClientError>();
        errorMock.Setup(e => e.Message).Returns("An error occurred");
        var errorsList = new List<IClientError> { errorMock.Object };

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Errors).Returns(errorsList);

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<GraphQLClientException>(() => sut.RunAsync(CancellationToken.None));

        _matchPersonRecordOrchestratorMock.Verify(
            o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldProcessEmptyList_WhenDataOrResultsAreNull()
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

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns((IPersonByCriteriaResult?)null);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        Assert.Empty(capturedRecords);
        _matchPersonRecordOrchestratorMock.Verify(
            o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), "graphql_extract",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldProcessEmptyList_WhenPersonByCriteriaIsNull()
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

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns((IPersonByCriteria_PersonByCriteria?)null);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        Assert.Empty(capturedRecords);
    }

    [Fact]
    public async Task RunAsync_ShouldProcessEmptyList_WhenResultsListIsEmpty()
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

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results)
            .Returns((IReadOnlyList<IPersonByCriteria_PersonByCriteria_Results>?)null);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        Assert.Empty(capturedRecords);
    }

    [Fact]
    public async Task RunAsync_ShouldMapNullFieldsToEmptyString_WhenPersonDataIsMissing()
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

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-null-fields");
        personMock.Setup(p => p.Forename).Returns((string?)null);
        personMock.Setup(p => p.Surname).Returns((string?)null);
        personMock.Setup(p => p.DateOfBirth).Returns((IPersonByCriteria_PersonByCriteria_Results_DateOfBirth?)null);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());
        personMock.Setup(p => p.PreferredAddress)
            .Returns((IPersonByCriteria_PersonByCriteria_Results_PreferredAddress?)null);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.Equal("person-null-fields", record.Record["SourceID"]);
        Assert.Equal("", record.Record["Forename"]);
        Assert.Equal("", record.Record["Surname"]);
        Assert.Equal("", record.Record["DOB"]);
        Assert.Equal("", record.Record["PostCode"]);
    }

    [Fact]
    public async Task RunAsync_ShouldCorrectlyParsePostcodeAndDob_WhenPresent()
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

        // Date of birth
        var dobMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_DateOfBirth>();
        dobMock.Setup(d => d.Lower).Returns((DateOnly?)null);

        // Preferred address mock
        var prefAddrMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_PreferredAddress>();
        prefAddrMock.Setup(a => a.Id).Returns("addr-pref");

        // Addresses
        var addrMock1 = new Mock<IPersonByCriteria_PersonByCriteria_Results_Addresses>();
        addrMock1.Setup(a => a.Id).Returns("addr-other");

        var locationMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Addresses_Location>();
        locationMock.Setup(l => l.Postcode).Returns("SW1A 1AA");

        var addrMock2 = new Mock<IPersonByCriteria_PersonByCriteria_Results_Addresses>();
        addrMock2.Setup(a => a.Id).Returns("addr-pref");
        addrMock2.Setup(a => a.Location).Returns(locationMock.Object);

        var addresses = new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>
        {
            addrMock1.Object, addrMock2.Object
        };

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-complex");
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns(dobMock.Object);
        personMock.Setup(p => p.Addresses).Returns(addresses.AsReadOnly());
        personMock.Setup(p => p.PreferredAddress).Returns(prefAddrMock.Object);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.Equal("", record.Record["DOB"]);
        Assert.Equal("SW1A 1AA", record.Record["PostCode"]);
    }

    [Fact]
    public async Task RunAsync_ShouldUseEmptyPostcode_WhenAddressLocationIsNull()
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

        var prefAddrMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_PreferredAddress>();
        prefAddrMock.Setup(a => a.Id).Returns("addr-pref");

        // Address matches preferred, but Location is null
        var addrMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Addresses>();
        addrMock.Setup(a => a.Id).Returns("addr-pref");
        addrMock.Setup(a => a.Location).Returns((IPersonByCriteria_PersonByCriteria_Results_Addresses_Location?)null);

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-null-loc");
        personMock.Setup(p => p.Addresses)
            .Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses> { addrMock.Object }.AsReadOnly());
        personMock.Setup(p => p.PreferredAddress).Returns(prefAddrMock.Object);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.Equal("", record.Record["PostCode"]);
    }

    [Fact]
    public async Task RunAsync_ShouldMapNhsNumberAndGender_WhenPresentInConfig()
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

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-optional-fields");
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns((IPersonByCriteria_PersonByCriteria_Results_DateOfBirth?)null);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());
        personMock.Setup(p => p.PreferredAddress)
            .Returns((IPersonByCriteria_PersonByCriteria_Results_PreferredAddress?)null);
        personMock.Setup(p => p.NhsNumber).Returns("1234567890");
        personMock.Setup(p => p.Gender).Returns(Gender.Male);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.Equal("1234567890", record.Record["NHSNumber"]);
        Assert.Equal(Gender.Male.ToString().ToLower(), record.Record["Gender"]);
    }

    [Fact]
    public async Task RunAsync_ShouldNotMapNhsNumberAndGender_WhenMissingFromConfig()
    {
        // Arrange
        var csvMatchOptionsNoOptionals = Options.Create(new CsvMatchDataOptions
        {
            DateFormat = "dd/MM/yyyy",
            ColumnMappings = new CsvMatchDataOptions.Headers
            {
                Id = "SourceID",
                Given = "Forename",
                Family = "Surname",
                BirthDate = "DOB",
                Postcode = "PostCode",
                NhsNumber = "", // Empty
                Gender = "" // Empty
            }
        });

        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });
        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            csvMatchOptionsNoOptionals
        );

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-no-optionals-config");
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns((IPersonByCriteria_PersonByCriteria_Results_DateOfBirth?)null);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());
        personMock.Setup(p => p.PreferredAddress)
            .Returns((IPersonByCriteria_PersonByCriteria_Results_PreferredAddress?)null);
        personMock.Setup(p => p.NhsNumber).Returns("1234567890");
        personMock.Setup(p => p.Gender).Returns(Gender.Male);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
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
        Assert.NotNull(capturedRecords);
        var record = Assert.Single(capturedRecords);
        Assert.False(record.Record.ContainsKey("NHSNumber"));
        Assert.False(record.Record.ContainsKey("Gender"));
    }

    [Fact]
    public async Task RunAsync_ShouldCallUpdatePersonMutation_WhenHighConfidenceMatchIsReturned()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });

        var updatePersonResultMock = new Mock<IUpdatePersonResult>();
        var operationUpdateResultMock = new Mock<IOperationResult<IUpdatePersonResult>>();
        operationUpdateResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        operationUpdateResultMock.Setup(r => r.Data).Returns(updatePersonResultMock.Object);

        var updatePersonMutationMock = new Mock<IUpdatePersonMutation>();
        updatePersonMutationMock
            .Setup(m => m.ExecuteAsync(It.IsAny<global::Eclipse.GraphQL.UpdatePerson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationUpdateResultMock.Object);

        _eclipseClientMock.Setup(c => c.UpdatePerson).Returns(updatePersonMutationMock.Object);

        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            _csvMatchOptions
        );

        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-123");
        personMock.Setup(p => p.ObjectVersion).Returns(5);
        personMock.Setup(p => p.PersonTypes).Returns(new List<PersonType> { PersonType.Client });
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns((IPersonByCriteria_PersonByCriteria_Results_DateOfBirth?)null);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());
        personMock.Setup(p => p.PreferredAddress)
            .Returns((IPersonByCriteria_PersonByCriteria_Results_PreferredAddress?)null);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock.Object);

        var matchResult = new Shared.Models.MatchResult
        {
            MatchStatus = Shared.Models.MatchStatus.Match,
            NhsNumber = "9999999999",
            Score = 1.0m
        };
        var apiResult = new Shared.Models.PersonMatchResponse { Result = matchResult };

        var matchedRecord = new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(new Dictionary<string, string>
            {
                { "SourceID", "person-123" },
                { "__ObjectVersion", "5" },
                { "NHSNumber", "" },
                { "__PersonTypes", "Client" }
            }),
            ApiResult = apiResult,
            IsSuccess = true
        };

        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>> { matchedRecord });

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        updatePersonMutationMock.Verify(
            m => m.ExecuteAsync(
                It.Is<global::Eclipse.GraphQL.UpdatePerson>(input =>
                    input.Id == "person-123" &&
                    input.NhsNumber == "9999999999" &&
                    input.ObjectVersion == 5 &&
                    input.PersonTypes != null &&
                    input.PersonTypes.Count == 1 &&
                    input.PersonTypes[0] == PersonType.Client),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCallUpdatePersonMutation_WhenPersonAlreadyHasNhsNumber()
    {
        // Arrange
        var options = Options.Create(new GraphQlProcessJobOptions { MaxAge = 25 });

        var updatePersonResultMock = new Mock<IUpdatePersonResult>();
        var operationUpdateResultMock = new Mock<IOperationResult<IUpdatePersonResult>>();
        operationUpdateResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        operationUpdateResultMock.Setup(r => r.Data).Returns(updatePersonResultMock.Object);

        var updatePersonMutationMock = new Mock<IUpdatePersonMutation>();
        updatePersonMutationMock
            .Setup(m => m.ExecuteAsync(It.IsAny<global::Eclipse.GraphQL.UpdatePerson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationUpdateResultMock.Object);

        _eclipseClientMock.Setup(c => c.UpdatePerson).Returns(updatePersonMutationMock.Object);

        var sut = new GraphQlProcessor(
            _loggerMock.Object,
            _eclipseClientMock.Object,
            _matchPersonRecordOrchestratorMock.Object,
            options,
            _csvMatchOptions
        );

        // Person already has NHS number "1111111111"
        var personMock = new Mock<IPersonByCriteria_PersonByCriteria_Results_Person>();
        personMock.Setup(p => p.Id).Returns("person-123");
        personMock.Setup(p => p.NhsNumber).Returns("1111111111");
        personMock.Setup(p => p.ObjectVersion).Returns(5);
        personMock.Setup(p => p.Forename).Returns("John");
        personMock.Setup(p => p.Surname).Returns("Doe");
        personMock.Setup(p => p.DateOfBirth).Returns((IPersonByCriteria_PersonByCriteria_Results_DateOfBirth?)null);
        personMock.Setup(p => p.Addresses).Returns(new List<IPersonByCriteria_PersonByCriteria_Results_Addresses>());
        personMock.Setup(p => p.PreferredAddress).Returns((IPersonByCriteria_PersonByCriteria_Results_PreferredAddress?)null);

        var resultsList = new List<IPersonByCriteria_PersonByCriteria_Results> { personMock.Object };

        var cursorMock = new Mock<IPersonByCriteria_PersonByCriteria_Cursor>();
        cursorMock.Setup(c => c.Offset).Returns(0);
        cursorMock.Setup(c => c.Returned).Returns(1);
        cursorMock.Setup(c => c.TotalSize).Returns(1);

        var personByCriteriaMock = new Mock<IPersonByCriteria_PersonByCriteria>();
        personByCriteriaMock.Setup(p => p.Results).Returns(resultsList.AsReadOnly());
        personByCriteriaMock.Setup(p => p.Cursor).Returns(cursorMock.Object);

        var operationResultDataMock = new Mock<IPersonByCriteriaResult>();
        operationResultDataMock.Setup(o => o.PersonByCriteria).Returns(personByCriteriaMock.Object);

        var operationResultMock = new Mock<IOperationResult<IPersonByCriteriaResult>>();
        operationResultMock.Setup(r => r.Data).Returns(operationResultDataMock.Object);
        operationResultMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        _personByCriteriaQueryMock
            .Setup(q => q.ExecuteAsync(It.IsAny<int>(), It.IsAny<RequestCursorInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationResultMock.Object);

        var matchResult = new Shared.Models.MatchResult
        {
            MatchStatus = Shared.Models.MatchStatus.Match,
            NhsNumber = "9999999999",
            Score = 1.0m
        };
        var apiResult = new Shared.Models.PersonMatchResponse
        {
            Result = matchResult
        };

        var matchedRecord = new ProcessedMatchRecord<CsvRecordDto>
        {
            OriginalData = new CsvRecordDto(new Dictionary<string, string>
            {
                { "SourceID", "person-123" },
                { "__ObjectVersion", "5" },
                { "NHSNumber", "1111111111" },
                { "__PersonTypes", "Client" }
            }),
            ApiResult = apiResult,
            IsSuccess = true
        };

        _matchPersonRecordOrchestratorMock
            .Setup(o => o.ProcessAsync(It.IsAny<IEnumerable<CsvRecordDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProcessedMatchRecord<CsvRecordDto>> { matchedRecord });

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert - Verify that ExecuteAsync is NEVER called because the person already has an NHS number
        updatePersonMutationMock.Verify(
            m => m.ExecuteAsync(It.IsAny<global::Eclipse.GraphQL.UpdatePerson>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}