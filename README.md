-# CSC_Single_Unique_Identifier
-
-Repo for the Single Unique Identifier team which is currently looking at testing the ability to match a persons records to the NHS number in order to understand if this could be implemented as a single unique identifier for children in the future.

Pre-reqs
https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli

Get V9

To build and run the project:
```
dotnet build sui-matching.sln
dotnet run --project app-host/AppHost.csproj
```
If you have errors connecting to the aspire host page you may need to run the below commands:
```
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

## Container diagram - C4 Model

```mermaid
C4Container
	title Container diagram for SUI Matcher

	System(laclient, "Local Authority Client", "The client to connect to the service")

	Container_Boundary(c1, "Sui Matching Service") {
		Container(reverse_proxy, "Gateway", "C#, .NET 8, YARP", "The reverse proxy/API gateway of the SUI matcher.")

		Container(validate_api, "Validate APIs", "C#, .NET 8, MassTransit", "The counter service.")
		Container(matching_api, "Matching APIs", "C#, .NET 8, MassTransit", "The barista service.")
		Container(auth_api, "Auth APIs", "C#, .NET 8, MassTransit", "The authentication service.")
		Container(external_api, "External Services", "C#, .NET 8, Marten", "Makes the outbound connections for other services")
		
		Boundary(b1, "Docker containers", "boundary") {
			ContainerDb(cache, "Storage", "Redis", "Stores bearer tokens for auth")
			Container(keyvault, "KeyVault", "Azure Key Vault", "Storage of secret values")
		}

	}
    Container_Boundary(c2, "NHS PDS FHIR API") {
		System(pds, "PDS FHIR API", "The PDS service")
    }

	Rel(laclient, reverse_proxy, "Uses", "HTTPS")
	
	Rel(reverse_proxy, validate_api, "Proxies", "HTTP")
	Rel(reverse_proxy, matching_api, "Proxies", "HTTP")

	Rel(auth_api, cache, "Adds/checks Tokens", "TCP")
    Rel(auth_api, keyvault, "Gets Secrets", "TCP")

    Rel(external_api, cache, "Gets Tokens", "TCP")
    Rel(external_api, keyvault, "Gets Secrets", "TCP")
    Rel(external_api, pds, "Retrieves NHS Number", "HTTPS")
	
	Rel(matching_api, auth_api, "Calls", "HTTP")
    Rel(matching_api, external_api, "Calls", "HTTP")
	
```

## Services:

### validate (external):
endpoint used to validate data send to the endpoint. Will return information about the validatity of the data sent to it. Should validate for the following data items:
given name (required)
family name (required)
gender
postcode
date of birth - which can be a range (required)
email address
phone number

Adapted from the schema specified here:
https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient

### matching (external):
Supplied with the information also supplied to the validate endpoint. It controls the logic for matching a single record. It crafts the request parameters to pass to the external api service in order to make the outbound call to the NHS.

### auth (internal):
Handles the secret key material in order to get the bearer token. It will maake its outbound connections via the external API. It will use azure keyvault to get the material needed to retrieve the bearer token. It will then store the bearer token in redis to be accessed by the external service.

Examples of how to build - https://github.com/NHSDigital/hello-world-auth-examples/tree/main/application-restricted-signed-jwt-tutorials/csharp

### external (internal):
Makes the external calls to the NHS authentication and NHS PDS endpoints. Will get secerts from keyvault and bearer token from Redis.

https://docs.fire.ly/projects/Firely-NET-SDK/en/latest/client/setup.html - should be implemented using this library.

### keyvault stub (local):
Currently a skeleton container in order to mimic azure keyvault in local testing. Unfortunately aspire does not provide an emulator for keyvault.

## Current Status of Repo
Scaffolding of services built out with an attempt to tie them together.