FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/tools/pds-emulator/PdsEmulator/PdsEmulator.csproj src/tools/pds-emulator/PdsEmulator/
RUN dotnet restore src/tools/pds-emulator/PdsEmulator/PdsEmulator.csproj

COPY src/tools/pds-emulator/PdsEmulator/ src/tools/pds-emulator/PdsEmulator/
RUN dotnet publish src/tools/pds-emulator/PdsEmulator/PdsEmulator.csproj \
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
ENTRYPOINT ["dotnet", "PdsEmulator.dll"]
