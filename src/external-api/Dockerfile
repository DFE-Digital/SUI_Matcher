FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/Shared/Shared.csproj src/Shared/
COPY src/external-api/External.csproj src/external-api/
RUN dotnet restore src/external-api/External.csproj

COPY src/Shared/ src/Shared/
COPY src/external-api/ src/external-api/
RUN dotnet publish src/external-api/External.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ARG ASPNETCORE_ENVIRONMENT=Production
ARG HTTP_PORTS=8080
ARG NHS_DIGITAL_TOKEN_URL
ARG NHS_DIGITAL_FHIR_ENDPOINT
ARG NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES=5

ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
    HTTP_PORTS=${HTTP_PORTS} \
    NhsAuthConfig__NHS_DIGITAL_TOKEN_URL=${NHS_DIGITAL_TOKEN_URL} \
    NhsAuthConfig__NHS_DIGITAL_FHIR_ENDPOINT=${NHS_DIGITAL_FHIR_ENDPOINT} \
    NhsAuthConfig__NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES=${NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES}

# Validate that the required environment variables are set at build time
RUN test -n "${NHS_DIGITAL_TOKEN_URL}" \
    && test -n "${NHS_DIGITAL_FHIR_ENDPOINT}"

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "External.dll"]
