#!/bin/bash

echo "Importing self-signed certificate for local development..."

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CERT_FILE="$SCRIPT_DIR/localhost_custom.crt"

if [ -f "$CERT_FILE" ]; then
    echo "Found $CERT_FILE."
else
    echo "Error: $CERT_FILE not found."
    echo "Please ensure the certificate file exists in the same directory as this script."
    exit 1
fi

if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux (Ubuntu/Debian)
    sudo cp "$CERT_FILE" /usr/local/share/ca-certificates/
    sudo update-ca-certificates
    echo "Certificate imported on Linux."
elif [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain "$CERT_FILE"
    echo "Certificate imported on macOS."
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    # Windows (via Git Bash or PowerShell)
    certutil -addstore -f "Root" "$CERT_FILE"
    echo "Certificate imported on Windows."
else
    echo "Unsupported OS: $OSTYPE. Please import $CERT_FILE manually."
    exit 1
fi

echo "Setup complete. You can now run the Docker container."