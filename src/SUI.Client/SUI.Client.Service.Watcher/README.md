# SUI Client Watcher Service

### Running the NuGet watcher tool from the command line
To run the tool, you can use the following command:

```bash
suiws --input <inputDirectory> --output <outputDirectory> --uri <absoluteUrlToService> --enable-gender <optional flag to use gender in search>
```

### Running the tool as a service
you must first copy the init.ps1 script from the nuget package to the tools directory.
To run the tool as a service, you can use the following command:
```powershell
.\init.ps1 -Input <inputDirectory> -Output <outputDirectory> -Uri <absoluteUrlToService> -EnableGender