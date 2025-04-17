# Readme for downloading and running the SUI Client Watcher tools from NuGet source

## Prerequisites
- .NET9 SDK installed on the machine running this application.
- Access to a terminal or command prompt.

## Downloading from Nuget source

To use these tools, you can install it from a NuGet source using the following command:

For the DBS Client Watcher:

```bash
dotnet tool install --global DFE.SUI.DBS.Client.Watcher --add-source <url to nuget source>
```

For the Client Watcher:
```bash
dotnet tool install --global DFE.SUI.Client.Watcher --add-source <url to nuget source>
```

## Running the watcher tool
To run the tool, you can use the following command:

For the Client Watcher:
```bash
suiw <inputDirectory> <outputDirectory> <absoluteUrlToMatchingService>
```

For the DBS Client Watcher:
```bash
suidbsw <inputDirectory> <outputDirectory>
```

## Update the tools
To update the tools, you can use the following command:
```bash
dotnet tool update --global DFE.SUI.Client.Watcher --add-source <url to nuget source>
```
```bash
dotnet tool update --global DFE.SUI.DBS.Client.Watcher --add-source <url to nuget source>
```

## Uninstall the tools
To uninstall the tools, you can use the following command:
```bash
dotnet tool uninstall --global DFE.SUI.DBS.Client.Watcher
```
```bash
dotnet tool uninstall --global DFE.SUI.Client.Watcher
```