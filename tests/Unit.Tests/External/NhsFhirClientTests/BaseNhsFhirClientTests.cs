using System.Net;

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
                Id = "123",
                Name =
                [
                    new HumanName("Smith", ["John"])
                    {
                        Period = null
                    },
                    new HumanName("Smith", ["John"])
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = null
                        }
                    },
                    new HumanName("Smith", ["John"])
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = "01/01/2002"
                        }
                    }
                ],
                Telecom =
                [
                    new ContactPoint(ContactPoint.ContactPointSystem.Email, ContactPoint.ContactPointUse.Home,
                        "test1@test.com")
                    {
                        Period = null
                    },
                    new ContactPoint(ContactPoint.ContactPointSystem.Email, ContactPoint.ContactPointUse.Home,
                        "test2@test.com")
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = null
                        }
                    },
                    new ContactPoint(ContactPoint.ContactPointSystem.Email, ContactPoint.ContactPointUse.Home,
                        "test3@test.com")
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = "01/01/2002"
                        }
                    },
                    new ContactPoint(ContactPoint.ContactPointSystem.Phone, ContactPoint.ContactPointUse.Home,
                        "0123456789")
                    {
                        Period = null
                    },
                    new ContactPoint(ContactPoint.ContactPointSystem.Phone, ContactPoint.ContactPointUse.Home,
                        "0123456780")
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = null
                        }
                    },
                    new ContactPoint(ContactPoint.ContactPointSystem.Phone, ContactPoint.ContactPointUse.Home,
                        "0123456785")
                    {
                        Period = new Period
                        {
                            Start = "01/01/2001",
                            End = "01/01/2002"
                        }
                    }
                ],
                Address =
                [
                    new Address { Period = new Period { Start = "2012-09-02", End = null }, PostalCode = "LS123EA", Use = Address.AddressUse.Home, Line = ["64 Higher Street", "Leeds", "West Yorkshire"] },
                    new Address { Period = new Period { Start = "2019-12-02", End = "2024-12-01"}, PostalCode = "LS123EH", Use = Address.AddressUse.Billing, Line = ["54 Medium Street", "Leeds", "West Yorkshire"] },
                    new Address { Period = new Period { Start = "2012-09-02", End = "2019-12-01"}, PostalCode = "LS123EG", Use = Address.AddressUse.Work, Line = ["34 Low Street", "Leeds", "West Yorkshire"] },
                    new Address { Period = new Period { Start = "2010-09-01", End = "2012-09-01"}, PostalCode = "LS123EF", Use = Address.AddressUse.Temp, Line = ["12 High Street", "Leeds", "West Yorkshire"] }
                ],
                GeneralPractitioner = [
                new ResourceReference {
                    Type = "Organization",
                    Identifier = new Identifier
                    {
                        System = "https://fhir.nhs.uk/Id/ods-organization-code",
                        Value = "Y12345",
                        Period = new Period{
                            Start = "2020-01-01",
                            End = null
                        }
                    }
                }
                ],
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

    protected class UnknownCaseFhirClient : FhirClient
    {
        public UnknownCaseFhirClient(string endpoint, FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
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
                    Code = OperationOutcome.IssueType.Conflict
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
    private readonly string _errorCode;

    public TestFhirClientError(string endpoint, string errorCode = "", FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
    {
        _errorCode = errorCode;
    }

    public override Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        throw new Exception("Error occurred while performing search");
    }

    public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
        CancellationToken? ct = null) where TResource : class
    {

        throw new FhirOperationException("Error occurred while performing search", HttpStatusCode.BadRequest, new OperationOutcome
        {
            Issue = [new OperationOutcome.IssueComponent { Details = new CodeableConcept("FHIR", _errorCode) }]
        });
    }
}