# Readme for downloading and running the SUI Client Watcher tools from NuGet source

## Prerequisites
- .NET9 SDK installed on the machine running this application. This may require administrative permission.
- Access to a terminal or command prompt.

## Downloading from Nuget source

To use these tools, you can install it from a NuGet source with or without a specific version using the following command:
Add a `--version <version>` parameter to the command to install a specific version.

For the Client Watcher:
```bash
dotnet tool install --global DFE.SUI.Client.Watcher
```

For the DBS Client Watcher:

```bash
dotnet tool install --global DFE.SUI.DBS.Response.Logger.Watcher
```

## Running the watcher tool
To run the tool, you can use the following command:

For the Client Watcher:
```bash
suiw <inputDirectory> <outputDirectory> <absoluteUrlToMatchingService>
```

For the Reconciliation Client Watcher:
```bash
suiw <inputDirectory> <outputDirectory> <absoluteUrlToMatchingService> --enable-reconciliation -s <strategy>
```

For the DBS Client Watcher:
```bash
suidbsw <inputDirectory> <outputDirectory>
```

## Update the tools
To update the tools, you can use the following command:
Add a `--version <version>` parameter to the command to install a specific version. Without version is will install the latest version.

```bash
dotnet tool update --global DFE.SUI.Client.Watcher
```
```bash
dotnet tool update --global DFE.SUI.DBS.Response.Logger.Watcher
```

## Uninstall the tools
To uninstall the tools, you can use the following command:

```bash
dotnet tool uninstall --global DFE.SUI.Client.Watcher
```
```bash
dotnet tool uninstall --global DFE.SUI.DBS.Response.Logger.Watcher
```

## Installing Windows 2022 server
```bash
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'dotnet-install.ps1';

./dotnet-install.ps1 -Version 9.0.300 -InstallDir 'C:\Program Files\dotnet\'

dotnet nuget add source "https://api.nuget.org/v3/index.json" --name "nuget.org"

dotnet tool install SUI.Client.Watcher --tool-path 'C:\Program Files\dotnet\tools'

$env:Path = "C:\Program Files\dotnet\tools;$env:Path"
```

## Reconciliation Output
When reconciliation completes it generates and output csv and pdf report. 
In the CSV there is a differences column. This is what those differences mean...
- fieldname:Both - this means that the field is missing in both the CSV and the NHS Demographic data
- fieldname:LA - this means the field is missing in the CSV
- fieldname:NHS - this means the data is missing in the NHS data
- fieldname - this means that the data is present in both sides but the data is different

if a field name does not appear in this list then the data was the same on both sides