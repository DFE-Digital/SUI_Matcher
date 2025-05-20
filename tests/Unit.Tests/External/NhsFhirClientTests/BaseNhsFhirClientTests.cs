using ExternalApi.Services;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

using Microsoft.Extensions.Logging;

using Moq;

using Task = System.Threading.Tasks.Task;

namespace Unit.Tests.External.NhsFhirClientTests;

public class BaseNhsFhirClientTests
{
    protected readonly Mock<ILogger<NhsFhirClient>> LoggerMock;
    protected readonly Mock<IFhirClientFactory> FhirClientFactory;

    protected BaseNhsFhirClientTests()
    {
        LoggerMock = new Mock<ILogger<NhsFhirClient>>();
        FhirClientFactory = new Mock<IFhirClientFactory>();
    }

    protected class TestFhirClientSuccess : FhirClient
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
                    new()
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

        public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
            CancellationToken? ct = null) where TResource : class
        {
            var resource = new Patient
            {
                Id = "123"
            } as TResource;
            return Task.FromResult<TResource?>(resource);
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
}

public class TestFhirClientError : FhirClient
{
    public TestFhirClientError(string endpoint, FhirClientSettings settings = null, HttpMessageHandler messageHandler = null) : base(endpoint, settings, messageHandler)
    {
    }
    
    public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        throw new Exception("Error occurred while performing search");
    }
    
    public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
        CancellationToken? ct = null) where TResource : class
    {
      
        throw new Exception("Error occurred while performing read");
    }
}

