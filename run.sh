#!/bin/bash

echo ""
echo "========================================"
echo "  SPOTIFY PLAYLIST DOWNLOADER - C#"
echo "========================================"
echo ""

if ! command -v dotnet &> /dev/null
then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 from: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo ""
    exit 1
fi

echo "Found .NET SDK"
echo ""

echo "[1/2] Checking yt-dlp..."
if ! command -v yt-dlp &> /dev/null
then
    echo ""
    echo "WARNING: yt-dlp not found!"
    echo "Please install: pip install yt-dlp"
    echo ""
fi

echo "[2/2] Starting application..."
echo ""
echo "Opening browser at: http://localhost:5000"
echo "Press Ctrl+C to stop the server"
echo ""

sleep 2

if command -v xdg-open &> /dev/null
then
    xdg-open http://localhost:5000 &
elif command -v open &> /dev/null
then
    open http://localhost:5000 &
fi

dotnet run
