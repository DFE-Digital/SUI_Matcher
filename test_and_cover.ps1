# Ensure cross-platform compatibility
$ErrorActionPreference = "Stop"

# Define directories
$resultsDir = "./coverage"
$mergedReport = "$resultsDir/coverage.xml"
$openCoverReport = "$resultsDir/opencover.xml"
$finalReportDir = "$resultsDir/coveragereport"


# First remove the old coverage directory if it exists to avoid skewed results
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

dotnet build --no-incremental

# Run tests with coverage collection
dotnet test --no-build --results-directory $resultsDir --collect:"XPlat Code Coverage" --settings tests.runsettings

# Merge all cobertura coverage reports
dotnet coverage merge --reports "$resultsDir/**/coverage.cobertura.xml" -f cobertura -o $mergedReport

# Generate XML report (which will be in OpenCover format)
dotnet coverage merge --reports "$resultsDir/**/coverage.cobertura.xml" -f xml -o $openCoverReport

# Generate HTML report from both formats
reportgenerator -reports:"$mergedReport;$openCoverReport" -reporttypes:Html -targetdir:$finalReportDir

open "$finalReportDir/index.html" # Open the report in the default browser

