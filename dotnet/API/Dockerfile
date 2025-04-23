# Stage 1: Build and Test
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY API/API.csproj ./API/
COPY API.Tests/API.Tests.csproj ./API.Tests/
RUN dotnet restore ./API/API.csproj
RUN dotnet restore ./API.Tests/API.Tests.csproj

# Copy the rest of the code
COPY . .

# Build the solution
RUN dotnet build ./API/API.csproj -c Release

# Run tests
RUN dotnet test ./API.Tests/API.Tests.csproj -c Release --no-build --verbosity normal

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish ./API/API.csproj -c Release -o /app/publish --no-build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

# Port for Azure Container Apps
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Debug aid in CI
RUN ls -la

ENTRYPOINT ["dotnet", "API.dll"]
