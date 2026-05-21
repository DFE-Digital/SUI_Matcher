FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/Shared/Shared.csproj src/Shared/
COPY src/SUI.Client/SUI.Client.Core/SUI.Client.Core.csproj src/SUI.Client/SUI.Client.Core/
COPY src/SUI.Client/SUI.Client.StorageProcessJob/SUI.Client.StorageProcessJob.csproj src/SUI.Client/SUI.Client.StorageProcessJob/
RUN dotnet restore src/SUI.Client/SUI.Client.StorageProcessJob/SUI.Client.StorageProcessJob.csproj

COPY src/Shared/ src/Shared/
COPY src/SUI.Client/SUI.Client.Core/ src/SUI.Client/SUI.Client.Core/
COPY src/SUI.Client/SUI.Client.StorageProcessJob/ src/SUI.Client/SUI.Client.StorageProcessJob/
RUN dotnet publish src/SUI.Client/SUI.Client.StorageProcessJob/SUI.Client.StorageProcessJob.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ARG CSV_DATE_FORMAT=yyyy-MM-dd
ARG MATCH_API_BASE_ADDRESS
ARG CSV_COLUMN_ID=Id
ARG CSV_COLUMN_GIVEN=GivenName
ARG CSV_COLUMN_FAMILY=FamilyName
ARG CSV_COLUMN_BIRTH_DATE=DOB
ARG CSV_COLUMN_EMAIL=EMAIL
ARG CSV_COLUMN_POSTCODE=POSTCODE
ARG CSV_COLUMN_GENDER=GENDER
ARG CSV_COLUMN_PHONE=PHONE
ARG CSV_COLUMN_NHS_NUMBER=NHS_NUMBER

ENV DOTNET_ENVIRONMENT=Production \
    StorageProcessJob__MatchApiBaseAddress=${MATCH_API_BASE_ADDRESS} \
    CsvMatchData__DateFormat=${CSV_DATE_FORMAT} \
    CsvMatchData__ColumnMappings__Id=${CSV_COLUMN_ID} \
    CsvMatchData__ColumnMappings__Given=${CSV_COLUMN_GIVEN} \
    CsvMatchData__ColumnMappings__Family=${CSV_COLUMN_FAMILY} \
    CsvMatchData__ColumnMappings__BirthDate=${CSV_COLUMN_BIRTH_DATE} \
    CsvMatchData__ColumnMappings__Email=${CSV_COLUMN_EMAIL} \
    CsvMatchData__ColumnMappings__Postcode=${CSV_COLUMN_POSTCODE} \
    CsvMatchData__ColumnMappings__Gender=${CSV_COLUMN_GENDER} \
    CsvMatchData__ColumnMappings__Phone=${CSV_COLUMN_PHONE} \
    CsvMatchData__ColumnMappings__NhsNumber=${CSV_COLUMN_NHS_NUMBER}

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SUI.Client.StorageProcessJob.dll"]
