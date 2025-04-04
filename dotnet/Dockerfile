# This image is an official Microsoft .NET SDK (Software Development Kit) image, based on Debian (similar to Ubuntu).
# It includes the full .NET 8.0 SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Define build arguments for Git config
ARG GIT_USER_NAME
ARG GIT_USER_EMAIL

# Set environment variables from build args (optional, for runtime use)
ENV GIT_USER_NAME=${GIT_USER_NAME}
ENV GIT_USER_EMAIL=${GIT_USER_EMAIL}

# Install dependencies and configure Git using the build args
RUN apt-get update && apt-get install -y \
    curl unzip git python3 python3-pip python3-venv build-essential ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && if [ -n "$GIT_USER_NAME" ] && [ -n "$GIT_USER_EMAIL" ]; then \
         git config --global user.name "$GIT_USER_NAME" && \
         git config --global user.email "$GIT_USER_EMAIL"; \
       else \
         echo "Warning: Git user.name and user.email not set. Provide GIT_USER_NAME and GIT_USER_EMAIL during build."; \
       fi 

# Install dotnet-ef globally:
RUN /usr/share/dotnet/dotnet tool install --ignore-failed-sources --add-source https://api.nuget.org/v3/index.json --global dotnet-ef \
    && echo 'export PATH="$PATH:/root/.dotnet/tools"' | tee -a /root/.bashrc /root/.profile

# Install libsecret (To be able to log in to Azure from Rider)
RUN apt-get update && apt-get install -y libsecret-1-0 libsecret-1-dev

# Copy all solution folder contents to the image
COPY . .
RUN dotnet restore MAL-Microservice.sln

# Install packages relevant for machine learning and Azure:
RUN python3 -m venv /venv \
    && . /venv/bin/activate \
    && pip install pythonnet==3.0.5 azureml-core azureml-mlflow

# Create a directory for SSL certificates:
RUN mkdir -p /https

# SSL Certificates should be defined in the lines below here:
COPY .certs/localhost_custom.pfx /https/localhost_custom.pfx
COPY .certs/localhost_custom.crt /src/localhost_custom.crt
RUN chmod 644 /https/localhost_custom.pfx

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/localhost_custom.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=DevPassword

#HTTP port
EXPOSE 8080 

#HTTPS port
EXPOSE 8081 

CMD ["/bin/bash"]
