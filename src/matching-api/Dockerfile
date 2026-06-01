FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/Shared/Shared.csproj src/Shared/
COPY src/matching-api/Matching.csproj src/matching-api/
RUN dotnet restore src/matching-api/Matching.csproj

COPY src/Shared/ src/Shared/
COPY src/matching-api/ src/matching-api/
RUN dotnet publish src/matching-api/Matching.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ARG ASPNETCORE_ENVIRONMENT=Production
ARG HTTP_PORTS=8080
ARG EXTERNAL_API_HTTP_ENDPOINT=http://external-api
ARG EXTERNAL_API_HTTPS_ENDPOINT=https://external-api

ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
    HTTP_PORTS=${HTTP_PORTS} \
    services__external-api__http__0=${EXTERNAL_API_HTTP_ENDPOINT} \
    services__external-api__https__0=${EXTERNAL_API_HTTPS_ENDPOINT}

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Matching.dll"]
