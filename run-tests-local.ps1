# dotnet test --settings tests.runsettings --collect:"XPlat Code Coverage" && reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html" && open coverage-report/index.html
#
dotnet test --settings tests.runsettings --collect:"XPlat Code Coverage" && reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html" && open coverage-report/index.html
