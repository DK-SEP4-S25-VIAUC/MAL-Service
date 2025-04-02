# Stage 1: Build and Test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY prototype/MAL-Api-Service/*.csproj ./MAL-Api-Service/
COPY prototype/MAL-Api-Service.Tests/*.csproj ./MAL-Api-Service.Tests/
RUN dotnet restore ./MAL-Api-Service/MAL-Api-Service.csproj
RUN dotnet restore ./MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj
COPY prototype/ .
COPY .certs/ /app/.certs/
RUN dotnet build ./MAL-Api-Service/MAL-Api-Service.csproj -c Release
RUN dotnet test ./MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj -c Release --verbosity normal

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish ./MAL-Api-Service/MAL-Api-Service.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /app/.certs/ ./.certs/
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

# Verify contents for debugging
RUN ls -la

ENTRYPOINT ["dotnet", "MAL-Api-Service.dll"]