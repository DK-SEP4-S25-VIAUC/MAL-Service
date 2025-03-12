# Base image: lightweight Linux with .NET SDK and Rider support
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

# Install Rider dependencies
#RUN apt-get update && \
 #   apt-get install -y \
 #   curl unzip git && \
 #   rm -rf /var/lib/apt/lists/*

# Install system dependencies
RUN apk add --no-cache curl unzip git python3 py3-pip

# Set the working directory
WORKDIR /app

# Copy project files and restore dependencies
#COPY MAL-Api-Service/*.csproj MAL-Api-Service/
#COPY MAL-Api-Service.Tests/*.csproj MAL-Api-Service.Tests/
COPY . .
RUN dotnet restore

# Add required libraries
RUN dotnet add MAL-Api-Service/MAL-Api-Service.csproj package coverlet.collector --version 6.0.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.14 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.Net.Test.Sdk --version 17.8.0 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package xunit --version 2.9.3 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package xunit.runner.visualstudio --version 2.5.3 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package Moq --version 4.20.72 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Swashbuckle.AspNetCore --version 7.3.0 \
    && dotnet add MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj package FluentAssertions --version 8.1.1 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.AspNetCore.Hosting --version 2.3.0 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Microsoft.EntityFrameworkCore --version 9.0.3 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package Newtonsoft.Json --version 13.0.3 \
    && dotnet add MAL-Api-Service/MAL-Api-Service.csproj package RestSharp --version 112.1.0 \
    && pip3 install pythonnet==3.0.5

# Restore all dependencies
RUN dotnet restore

# Expose ports
EXPOSE 8000 8001

# Default command
CMD ["dotnet", "watch", "run"]