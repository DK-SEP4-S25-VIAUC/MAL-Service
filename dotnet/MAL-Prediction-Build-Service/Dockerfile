# Stage 1: Build and Test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and all project files within MAL-Prediction-Build-Service to restore dependencies
COPY . .

# Restore dependencies for the MAL-Prediction-Build-Service solution
RUN dotnet restore MAL-Microservice.sln

# Copy the source code for MAL-Prediction-Build-Service only
COPY MAL-Prediction-Build-Service/ MAL-Prediction-Build-Service/

# Build the PredictionBuildService project
RUN dotnet build MAL-Prediction-Build-Service/PredictionBuildService/PredictionBuildService.csproj -c Release

# Run tests
RUN dotnet test MAL-Prediction-Build-Service/PredictionBuildService.Tests/PredictionBuildService.Tests.csproj -c Release --verbosity normal
RUN dotnet test MAL-Prediction-Build-Service/PredictionBuildService.Infrastructure.Tests/PredictionBuildService.Infrastructure.Tests.csproj -c Release --verbosity normal
RUN dotnet test MAL-Prediction-Build-Service/PredictionBuildService.core.Tests/PredictionBuildService.core.Tests.csproj -c Release --verbosity normal

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish MAL-Prediction-Build-Service/PredictionBuildService/PredictionBuildService.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Verify contents for debugging
RUN ls -la

ENTRYPOINT ["dotnet", "PredictionBuildService.dll"]