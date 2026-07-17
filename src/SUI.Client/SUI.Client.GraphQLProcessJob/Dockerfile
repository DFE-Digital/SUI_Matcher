FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props NuGet.config ./
COPY src/SUI.Client/SUI.Client.Core/SUI.Client.Core.csproj src/SUI.Client/SUI.Client.Core/
COPY src/SUI.Client/SUI.Client.GraphQLProcessJob/SUI.Client.GraphQLProcessJob.csproj src/SUI.Client/SUI.Client.GraphQLProcessJob/
RUN dotnet restore src/SUI.Client/SUI.Client.GraphQLProcessJob/SUI.Client.GraphQLProcessJob.csproj

COPY src/SUI.Client/SUI.Client.Core/ src/SUI.Client/SUI.Client.Core/
COPY src/SUI.Client/SUI.Client.GraphQLProcessJob/ src/SUI.Client/SUI.Client.GraphQLProcessJob/
RUN dotnet publish src/SUI.Client/SUI.Client.GraphQLProcessJob/SUI.Client.GraphQLProcessJob.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "SUI.Client.GraphQLProcessJob.dll"]
