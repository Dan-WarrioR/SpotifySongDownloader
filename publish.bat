@echo off
echo.
echo ========================================
echo   BUILD SELF-CONTAINED EXECUTABLE
echo ========================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found.
    echo Install from: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo Cleaning previous build...
if exist "dist" rmdir /s /q "dist"
echo.

echo Publishing...
dotnet publish -r win-x64 -c Release --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o dist

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build failed.
    pause
    exit /b 1
)

echo.
echo Creating launch script in dist\...
(
    echo @echo off
    echo SpotifyDownloader.exe
) > dist\launch.bat

echo.
echo ========================================
echo   DONE — dist\ is ready
echo ========================================
echo.
echo The dist\ folder is fully self-contained.
echo Copy it anywhere — no .NET installation needed.
echo.
echo Before running, drop these into dist\:
echo   yt-dlp.exe   ^(or ensure it is in PATH^)
echo   ffmpeg.exe   ^(or ensure it is in PATH^)
echo.
echo Then run: dist\launch.bat
echo.
pause