# SUI DBS Client Watcher

## Creating NuGet package

https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create

Run the following CLI command to create a NuGet package:

```bash
dotnet pack --configuration Release --output ./nupkg /p:PackAsTool=true /p:Version=1.0.0 /p:ToolCommandName=suidbsw
```

This will create a NuGet package in the `nupkg` directory with the specified version and configuration.
The `/p:ToolCommandName=suidbsw` option sets the command name for the tool

## Installing the NuGet package locally

To install the NuGet package locally, you can use the following command:

```bash
dotnet tool install --global --add-source ./nupkg DFE.SUI.DBS.Client.Watcher --version 1.0.0
```

## Uninstalling the NuGet package
To uninstall the NuGet package, you can use the following command:

```bash
dotnet tool uninstall --global DFE.SUI.DBS.Client.Watcher
```

### Running the NuGet watcher tool from the command line
To run the tool, you can use the following command:

```bash
suidbsw <inoutDirectory> <outputDirectory>
```