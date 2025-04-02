@echo off
echo Importing self-signed certificate for local development...

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"
REM Set the full path to the certificate file with proper quoting
set "CERT_FILE=%SCRIPT_DIR%localhost_custom.crt"

echo Checking for "%CERT_FILE%"...
REM Check if the certificate file exists
if exist "%CERT_FILE%" (
    echo Found "%CERT_FILE%".
) else (
    echo Error: "%CERT_FILE%" not found in the .certs/ folder.
    echo Please ensure the certificate file exists in the same directory as this script.
    echo Current working directory: %CD%
    pause
    exit /b 1
)

REM Import the certificate into the Trusted Root Certification Authorities store
echo Running certutil to import the certificate...
certutil -addstore -f "Root" "%CERT_FILE%"
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to import the certificate. Error level: %ERRORLEVEL%
    echo Try running this script as Administrator.
    echo Current user: %USERNAME%
    pause
    exit /b 1
)

echo Certificate imported successfully.
echo You can now run the Docker container.
echo Press any key to exit...
pause >nul