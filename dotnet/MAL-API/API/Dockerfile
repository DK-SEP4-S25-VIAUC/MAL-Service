# Stage 1: Build and Test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY . .

# Restore dependencies
RUN dotnet restore MAL-Microservice.sln


# Copy the source code for the API project
COPY MAL-API/ MAL-API/
COPY .certs/ /app/.certs/

# Build the solution
RUN dotnet build MAL-API/API/API.csproj -c Release

# Run tests
RUN dotnet test MAL-API/API.tests/API.tests.csproj -c Release --verbosity normal

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish MAL-API/API/API.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /app/.certs/ ./.certs/

# Port for Azure Container Apps
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Debug aid in CI
RUN ls -la

ENTRYPOINT ["dotnet", "API.dll"]
