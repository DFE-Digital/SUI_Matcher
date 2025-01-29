-# CSC_Single_Unique_Identifier
-
-Repo for the Single Unique Identifier team which is currently looking at testing the ability to match a persons records to the NHS number in order to understand if this could be implemented as a single unique identifier for children in the future.

Pre-reqs
https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli

get V9
```
dotnet build sui-matching.sln
dotnet run --project app-host/AppHost.csproj

dotnet dev-certs https --clean
dotnet dev-certs https --trust
```