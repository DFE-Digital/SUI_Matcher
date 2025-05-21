using ExternalApi.Services;

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

using Microsoft.Extensions.Logging;

using Moq;

using Task = System.Threading.Tasks.Task;

namespace Unit.Tests.External.NhsFhirClientTests;

public class BaseNhsFhirClientTests
{
    protected readonly Mock<ILogger<NhsFhirClient>> _loggerMock;
    protected readonly Mock<IFhirClientFactory> _fhirClientFactory;

    protected BaseNhsFhirClientTests()
    {
        _loggerMock = new Mock<ILogger<NhsFhirClient>>();
        _fhirClientFactory = new Mock<IFhirClientFactory>();
    }

    protected class TestFhirClientSuccess : FhirClient
    {
        public TestFhirClientSuccess(string endpoint, FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
        {
        }

        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            var bundle = new Bundle
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
            
            return await Task.FromResult<Bundle?>(bundle);
        }

        public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
            CancellationToken? ct = null) where TResource : class
        {
            var resource = new Patient
            {
                Id = "123"
            } as TResource;
            return Task.FromResult(resource);
        }
    }

    protected class TestFhirClientMultiMatch : FhirClient
    {
        public TestFhirClientMultiMatch(string endpoint, FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
        {
        }

        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            return await Task.FromResult<Bundle?>(null);
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

    protected class TestFhirClientUnmatched : FhirClient
    {
        public TestFhirClientUnmatched(string endpoint, FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
        {
        }

        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            var bundle = new Bundle
            {
                Entry = []
            };
            
            return await Task.FromResult<Bundle?>(bundle);
        }
    }
}

public class TestFhirClientError : FhirClient
{
    public TestFhirClientError(string endpoint, FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
    {
    }

    public override  Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        throw new Exception("Error occurred while performing search");
    }

    public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
        CancellationToken? ct = null) where TResource : class
    {

        throw new Exception("Error occurred while performing read");
    }
}