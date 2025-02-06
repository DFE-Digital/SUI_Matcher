# CSC_Single_Unique_Identifier

Repo for the Single Unique Identifier team which is currently looking at testing the ability to match a persons records to the NHS number in order to understand if this could be implemented as a single unique identifier for children in the future.

Pre-reqs
https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli

Install .net SDK v9. Instructions for MacOS below.

```curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 9.0.102 --install-dir "$HOME/.dotnet
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc && source ~/.zshrc && echo $PATH
```

To build and run the project:
```
dotnet build sui-matching.sln
dotnet run --project app-host/AppHost.csproj
```
Run simple test:
```
curl -vv http://localhost:5000/validate/api/v1/runvalidation
```
If you have errors connecting to the aspire host page you may need to run the below commands:
```
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
