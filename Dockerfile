﻿FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Define build arguments for Git config
ARG GIT_USER_NAME
ARG GIT_USER_EMAIL

# Set environment variables from build args (optional, for runtime use)
ENV GIT_USER_NAME=${GIT_USER_NAME}
ENV GIT_USER_EMAIL=${GIT_USER_EMAIL}

# Install dependencies and configure Git using the build args
RUN apt-get update && apt-get install -y \
    curl \
    unzip \
    git \
    python3 \
    python3-pip \
    python3-venv \
    build-essential \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && if [ -n "$GIT_USER_NAME" ] && [ -n "$GIT_USER_EMAIL" ]; then \
         git config --global user.name "$GIT_USER_NAME" && \
         git config --global user.email "$GIT_USER_EMAIL"; \
       else \
         echo "Warning: Git user.name and user.email not set. Provide GIT_USER_NAME and GIT_USER_EMAIL during build."; \
       fi

# TODO: Make sure to add future projects in this solution, to the list below so they are properly built upon new Dockerfile build.
COPY MAL-Microservice.sln .
COPY MAL-Api-Service/MAL-Api-Service.csproj MAL-Api-Service/
COPY MAL-Api-Service/appsettings.json MAL-Api-Service/
COPY MAL-Api-Service.Tests/MAL-Api-Service.Tests.csproj MAL-Api-Service.Tests/

RUN dotnet restore MAL-Microservice.sln

# Install packages relevant for machine learning and Azure:
RUN python3 -m venv /venv \
    && . /venv/bin/activate \
    && pip install pythonnet==3.0.5 azureml-core azureml-mlflow

RUN mkdir -p /https

# SSL Certificates should be defined in the lines below here:
COPY .certs/localhost_custom.pfx /https/localhost_custom.pfx
COPY .certs/localhost_custom.crt /src/localhost_custom.crt
RUN chmod 644 /https/localhost_custom.pfx

COPY . .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/localhost_custom.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=DevPassword

WORKDIR /src/MAL-Api-Service

#Define a VOLUME so workdata is persisted across containers and images:
VOLUME /rider_sep4_mal_volume

#HTTP port
EXPOSE 8080 

#HTTPS port
EXPOSE 8081 

CMD ["dotnet", "run", "--project", "MAL-Api-Service.csproj", "--urls", "http://0.0.0.0:8080;https://0.0.0.0:8081"]
