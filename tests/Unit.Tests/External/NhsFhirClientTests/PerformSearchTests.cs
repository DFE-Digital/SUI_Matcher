using ExternalApi.Services;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using Shared.Models;

using Task = System.Threading.Tasks.Task;

namespace Unit.Tests.External.NhsFhirClientTests;

[TestClass]
public class PerformSearchTests
{
    private readonly Mock<ILogger<NhsFhirClient>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IFhirClientFactory> _fhirClientFactory;
    
    public PerformSearchTests()
    {
        
        _loggerMock = new Mock<ILogger<NhsFhirClient>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["NhsAuthConfig:NHS_DIGITAL_FHIR_ENDPOINT"])
            .Returns("https://fhir.api.endpoint");
        
        _fhirClientFactory = new Mock<IFhirClientFactory>();
    }
    
    [TestMethod]
    public async Task ShouldGetSearchResultsMatched_WhenMatched()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "Smith",
            Given = ["John"],
            Gender = "male",
            Birthdate = ["eq1980-01-01"],
        };
        var testFhirClient = new TestFhirClientSuccess("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);
        
        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.Matched, result.Type);
        Assert.AreEqual("123", result.NhsNumber);
    }

    [TestMethod]
    public async Task ShouldGetSearchResultsMultiMatched_WhenMultipleMatches()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "Doe",
            Given = ["John"],
            Birthdate = ["eq1980-01-01"],
        };

        var testFhirClient = new TestFhirClientMultiMatch("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.MultiMatched, result.Type);
    }

    [TestMethod]
    public async Task ShouldGetSearchResultsUnmatched_WhenNoMatches()
    {
        // Arrange
        var searchQuery = new SearchQuery
        {
            Family = "NotExistent",
            Given = ["IAm"],
            Birthdate = ["eq1900-01-01"],
        };
        
        var testFhirClient = new TestFhirClientUnmatched("https://fhir.api.endpoint");
        _fhirClientFactory.Setup(f => f.CreateFhirClient())
            .Returns(testFhirClient);

        var client = new NhsFhirClient(_fhirClientFactory.Object, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.PerformSearch(searchQuery);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(SearchResult.ResultType.Unmatched, result.Type);
    }
}

public class TestFhirClientSuccess : FhirClient
{
    public TestFhirClientSuccess(string endpoint, FhirClientSettings settings = null, HttpMessageHandler messageHandler = null) : base(endpoint, settings, messageHandler)
    {
    }

    public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        return new Bundle
        {
            Entry = new List<Bundle.EntryComponent>()
            {
                new Bundle.EntryComponent
                {
                    Resource = new Patient
                    {
                        Id = "123"
                    },
                    Search = new Bundle.SearchComponent()
                    {
                        Mode = Bundle.SearchEntryMode.Match,
                        Score = 1.0m
                    }
                }
            },
        };
    }
}

public class TestFhirClientMultiMatch : FhirClient
{
    public TestFhirClientMultiMatch(string endpoint, FhirClientSettings settings = null, HttpMessageHandler messageHandler = null) : base(endpoint, settings, messageHandler)
    {
    }

    public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        return null;
    }

    public override Resource? LastBodyAsResource => new OperationOutcome()
    {
        Issue =
        [
            new OperationOutcome.IssueComponent()
            {
                Code = OperationOutcome.IssueType.MultipleMatches
            }
        ]
    };
}

public class TestFhirClientUnmatched : FhirClient
{
    public TestFhirClientUnmatched(string endpoint, FhirClientSettings settings = null, HttpMessageHandler messageHandler = null) : base(endpoint, settings, messageHandler)
    {
    }

    public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        return new Bundle
        {
            Entry = []
        };
    }
}