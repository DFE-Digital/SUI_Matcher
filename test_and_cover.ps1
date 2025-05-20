# Ensure cross-platform compatibility
$ErrorActionPreference = "Stop"

# Define directories
$resultsDir = "./coverage"
$mergedReport = "$resultsDir/coverage.xml"
$finalReportDir = "$resultsDir/coveragereport"

# Run tests with coverage collection
dotnet test --settings tests.runsettings --results-directory $resultsDir --diag mytestlog.txt

# Merge all cobertura coverage reports
dotnet coverage merge --reports "$resultsDir/**/coverage.cobertura.xml" -f cobertura -o $mergedReport

# Generate HTML report
reportgenerator -reports:$mergedReport -reporttypes:Html -targetdir:$finalReportDir

open "$finalReportDir/index.html" # Open the report in the default browser

