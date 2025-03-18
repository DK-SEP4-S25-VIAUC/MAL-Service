# Stage 1: Build and Test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MAL-Microservice/prototype/MAL-Api-Service/*.csproj ./MAL-Api-Service/
COPY MAL-Microservice/prototype/MAL-Api-Service.Tests/*.csproj ./MAL-Api-Service.Tests/
RUN dotnet restore ./MAL-Api-Service/MAL-Api-Service.csproj
RUN dotnet restore ./MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj
COPY MAL-Microservice/prototype .
RUN dotnet build ./MAL-Api-Service/MAL-Api-Service.csproj -c Release
RUN dotnet test ./MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj -c Release --no-build --verbosity normal

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish ./MAL-Api-Service/MAL-Api-Service.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MAL-Api-Service.dll"]