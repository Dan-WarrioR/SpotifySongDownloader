#!/bin/bash
echo ""
echo "========================================"
echo "  SPOTIFY PLAYLIST DOWNLOADER - C#"
echo "========================================"
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null
then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0+"
    echo ""
    echo "For Arch/CachyOS, run:"
    echo "  sudo pacman -S dotnet-sdk"
    echo ""
    echo "For other distros, visit: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo ""
    exit 1
fi

echo "✓ Found .NET SDK: $(dotnet --version)"
echo ""

# Check for yt-dlp
echo "[1/2] Checking yt-dlp..."
if ! command -v yt-dlp &> /dev/null
then
    echo ""
    echo "⚠ WARNING: yt-dlp not found!"
    echo ""
    echo "Install options:"
    echo "  Arch/CachyOS:  sudo pacman -S yt-dlp"
    echo "  pip:           pip install yt-dlp"
    echo "  pipx:          pipx install yt-dlp"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]
    then
        exit 1
    fi
else
    echo "✓ Found yt-dlp: $(yt-dlp --version)"
fi
echo ""

# Check for ffmpeg
echo "[2/2] Checking ffmpeg..."
if ! command -v ffmpeg &> /dev/null
then
    echo ""
    echo "⚠ WARNING: ffmpeg not found!"
    echo ""
    echo "Install options:"
    echo "  Arch/CachyOS:  sudo pacman -S ffmpeg"
    echo "  Ubuntu/Debian: sudo apt install ffmpeg"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]
    then
        exit 1
    fi
else
    echo "✓ Found ffmpeg: $(ffmpeg -version | head -n1)"
fi
echo ""

echo "Starting application..."
echo ""
echo "🌐 Opening browser at: http://localhost:5000"
echo "⏹️  Press Ctrl+C to stop the server"
echo ""
echo "========================================"
echo ""

# Wait a moment before opening browser
sleep 2

# Try to open browser (works on most Linux desktop environments)
if command -v xdg-open &> /dev/null
then
    xdg-open http://localhost:5000 &> /dev/null &
elif command -v kde-open &> /dev/null
then
    kde-open http://localhost:5000 &> /dev/null &
elif command -v gnome-open &> /dev/null
then
    gnome-open http://localhost:5000 &> /dev/null &
fi

# Run the application
dotnet run