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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless-extra AS final
WORKDIR /app

ARG MATCH_API_BASE_ADDRESS

ENV DOTNET_ENVIRONMENT=Production \
    StorageProcessJob__MatchApiBaseAddress=${MATCH_API_BASE_ADDRESS}

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SUI.Client.StorageProcessJob.dll"]
