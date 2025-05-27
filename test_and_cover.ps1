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


dotnet build --no-incremental
dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --settings tests.runsettings
reportgenerator -reports:./**/coverage.cobertura.xml -targetdir:$finalReportDir -reporttypes:SonarQube,html

# Merge all cobertura coverage reports (For legacy use on test reporting in CI)
dotnet coverage merge --reports "tests/**/coverage.cobertura.xml" -f cobertura -o $mergedReport

open "$finalReportDir/index.html" # Open the report in the default browser

