# Use the .NET 8.0 SDK image based on Ubuntu
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /src

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    unzip \
    git \
    python3 \
    python3-pip \
    build-essential \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy the solution, project files, and appsettings.json
COPY MAL-Microservice.sln .
COPY MAL-Api-Service/MAL-Api-Service.csproj MAL-Api-Service/
COPY MAL-Api-Service/appsettings.json MAL-Api-Service/
COPY MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj MAL-Api-Service.Tests/

# Restore dependencies for the solution
RUN dotnet restore MAL-Microservice.sln

# Install required .NET packages for MAL-Api-Service
RUN dotnet add MAL-Api-Service/MAL-Api-Service.csproj package coverlet.collector --version 6.0.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.14 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.Net.Test.Sdk --version 17.8.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Swashbuckle.AspNetCore --version 7.3.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.AspNetCore.Hosting --version 2.3.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.EntityFrameworkCore --version 9.0.3 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Newtonsoft.Json --version 13.0.3 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package RestSharp --version 112.1.0

# Install required .NET packages for MAL-Api-Service.Tests
RUN dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package xunit --version 2.9.3 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package xunit.runner.visualstudio --version 2.5.3 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package Moq --version 4.20.72 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package FluentAssertions --version 8.1.1

# Create a virtual environment for Python and install pythonnet
RUN python3 -m venv /venv \
    && . /venv/bin/activate \
    && pip install pythonnet==3.0.5

# Create the /https directory for the certificate
RUN mkdir -p /https

# Generate a self-signed certificate for development
RUN dotnet dev-certs https --export-path /https/aspnetapp.pfx --password "devpassword" \
    && chmod 644 /https/aspnetapp.pfx

# Copy the rest of the source code
COPY . .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=devpassword

# Set the working directory to the API service project
WORKDIR /src/MAL-Api-Service

# Expose ports (8080 for HTTP, 8081 for HTTPS)
EXPOSE 8080
EXPOSE 8081

# Command to run the application with HTTPS
CMD ["dotnet", "run", "--project", "MAL-Api-Service.csproj", "--urls", "http://0.0.0.0:8080;https://0.0.0.0:8081"]