# Ensure cross-platform compatibility
$ErrorActionPreference = "Stop"

# Define directories
$resultsDir = "./coverage"
$mergedReport = "$resultsDir/coverage.xml"
$finalReportDir = "$resultsDir/coveragereport"


# First remove the old coverage directory if it exists to avoid skewed results
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

# Remove all folders with name 'TestResults'
Get-ChildItem -Path . -Recurse -Directory -Filter "TestResults" | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
}


dotnet build
dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --settings tests.runsettings
reportgenerator -reports:./**/coverage.cobertura.xml -targetdir:$finalReportDir -reporttypes:SonarQube,html

open "$finalReportDir/index.html" # Open the report in the default browser

