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

ARG ASPNETCORE_ENVIRONMENT=Prod
ARG HTTP_PORTS=8080

ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
    HTTP_PORTS=${HTTP_PORTS}

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "External.dll"]
