@echo off
echo.
echo ========================================
echo   SPOTIFY PLAYLIST DOWNLOADER - C#
echo ========================================
echo.

dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 10.0 or 8.0+ from: https://dotnet.microsoft.com/download/dotnet
    echo Make sure to install the x64 version!
    echo.
    pause
    exit /b 1
)

echo Found .NET SDK
dotnet --version
echo.

echo [1/2] Checking yt-dlp...
yt-dlp --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo WARNING: yt-dlp not found!
    echo Please install: winget install yt-dlp
    echo.
)

echo [2/2] Starting application...
echo.
echo Opening browser at: http://localhost:5000
echo.
echo If the browser shows an error, check the console below for details.
echo Press Ctrl+C to stop the server
echo.

timeout /t 2 >nul
start http://localhost:5000

dotnet run
set EXIT_CODE=%ERRORLEVEL%

echo.
echo ========================================
if %EXIT_CODE% NEQ 0 (
    echo Application exited with error code: %EXIT_CODE%
    echo See error messages above for details
) else (
    echo Application stopped
)
echo ========================================
echo.
pause
