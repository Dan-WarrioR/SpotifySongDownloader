@echo off
echo.
echo ========================================
echo   SPOTIFY PLAYLIST DOWNLOADER - C#
echo ========================================
echo.

REM Check for .NET SDK
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0+ from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [OK] Found .NET SDK: %DOTNET_VERSION%
echo.

REM Check for yt-dlp
echo [1/2] Checking yt-dlp...
where yt-dlp >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    if not exist "yt-dlp.exe" (
        echo.
        echo [WARNING] yt-dlp not found!
        echo.
        echo Install options:
        echo   pip:      pip install yt-dlp
        echo   winget:   winget install yt-dlp
        echo   scoop:    scoop install yt-dlp
        echo   Or download from: https://github.com/yt-dlp/yt-dlp/releases
        echo.
        choice /C YN /M "Continue anyway"
        if errorlevel 2 exit /b 1
    ) else (
        echo [OK] Found yt-dlp.exe in current directory
    )
) else (
    for /f "tokens=*" %%i in ('yt-dlp --version') do set YTDLP_VERSION=%%i
    echo [OK] Found yt-dlp: %YTDLP_VERSION%
)
echo.

REM Check for ffmpeg
echo [2/2] Checking ffmpeg...
where ffmpeg >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    if not exist "ffmpeg.exe" (
        echo.
        echo [WARNING] ffmpeg not found!
        echo.
        echo Install options:
        echo   winget:   winget install ffmpeg
        echo   scoop:    scoop install ffmpeg
        echo   choco:    choco install ffmpeg
        echo   Or download from: https://ffmpeg.org/download.html
        echo.
        choice /C YN /M "Continue anyway"
        if errorlevel 2 exit /b 1
    ) else (
        echo [OK] Found ffmpeg.exe in current directory
    )
) else (
    echo [OK] Found ffmpeg in PATH
)
echo.

echo Starting application...
echo.
echo [WEB] Opening browser at: http://localhost:5000
echo [STOP] Press Ctrl+C to stop the server
echo.
echo ========================================
echo.

REM Wait a moment before opening browser
timeout /t 2 /nobreak >nul

REM Open default browser
start http://localhost:5000

REM Run the application
dotnet run