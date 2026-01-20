# 🎵 Spotify Playlist Downloader

A clean C# application to download Spotify playlists as MP3 files with album artwork.

![Platform](https://img.shields.io/badge/Platform-.NET%2010-512BD4?style=for-the-badge&logo=dotnet)
![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=csharp)

---

## 📋 What It Does

Downloads all tracks from your Spotify playlists as high-quality MP3 files with embedded album artwork. Automatically skips already downloaded tracks.

## ✨ Features

- 🎵 Download complete playlists
- 🖼️ Automatic album artwork embedding  
- ⚡ 3 concurrent downloads for speed
- 🔄 Smart duplicate detection
- 📊 Real-time progress tracking
- 💾 Download history

---

## 🚀 Quick Start

### 1️⃣ Requirements

Install these first:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (or .NET 8+)
- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)** - For downloading audio
- **[ffmpeg](https://ffmpeg.org/download.html)** - For audio processing

**Windows quick install:**
```powershell
winget install Microsoft.DotNet.SDK.10
winget install yt-dlp
winget install ffmpeg
```

### 2️⃣ Get Spotify Credentials

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Create a new app
3. Copy your **Client ID** and **Client Secret**

### 3️⃣ Run the Application

**Option A: Quick Run (Development)**
```bash
# Double-click run.bat (Windows) or run.sh (Mac/Linux)
# OR run this command:
dotnet run
```

Your browser will open at `http://localhost:5000`

**Option B: Build Executable (Recommended)**
```bash
# Build a single executable file
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./build

# The .exe will be in the ./build folder
# Just double-click SpotifyDownloader.exe to run!
```

For other platforms:
- **Linux**: Replace `win-x64` with `linux-x64`
- **Mac**: Replace `win-x64` with `osx-x64`

---

## 📖 How to Use

### Step 1: Configure
Open the app in your browser and enter:

- **Spotify Client ID** - From your Spotify app
- **Spotify Client Secret** - From your Spotify app  
- **Playlist ID** - Get from Spotify playlist URL
  - Example: `https://open.spotify.com/playlist/37i9dQZF1DX...`
  - The ID is the part after `playlist/`
- **Download Folder** - Where to save music
  - Windows: `C:\Users\YourName\Music\Spotify`
  - Mac: `/Users/YourName/Music/Spotify`
  - Linux: `/home/username/Music/Spotify`

💡 **Tip:** Copy folder paths from your file manager's address bar

Click **Save Configuration**

### Step 2: Download
Click **Start Download** and watch the progress!

The app shows:
- ✅ Current track being downloaded
- 📊 Progress percentage
- 📈 Completed/Failed/Total counts
- 📝 Real-time results list

---

## 📁 Project Structure

```
SpotifyDownloader/
├── run.bat / run.sh          # Launch scripts
├── SpotifyDownloader.csproj  # Project file
├── Data/                     # Config files (auto-created)
│   ├── config.json          # Your settings
│   └── downloaded_tracks.json # Download history
└── Scripts/                  # All C# source code
    ├── Boot.cs
    ├── Core/
    ├── Features/
    ├── Controllers/
    └── Data/
```

---

## 🛠️ Building an Executable

To create a standalone `.exe` that doesn't require `dotnet run`:

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./build
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./build
```

### Mac
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o ./build
```

The executable will be in the `build` folder. Just copy it anywhere and double-click to run!

---

## ❓ Troubleshooting

### "Application won't start"
- Check .NET is installed: `dotnet --version`
- Should show 10.0.x or 8.0.x+
- Make sure you have the **x64** version

### "Port already in use"
- Another app is using port 5000
- Change the port in `Scripts/Core/Application.cs`

### "Downloads fail"
- Verify yt-dlp and ffmpeg are installed
- Check Spotify credentials are correct
- Ensure playlist ID is valid
- Check internet connection

### "Can't find folder path"
**Windows:** Open folder → Click address bar → Copy path  
**Mac:** Right-click folder → Hold Option → "Copy as Pathname"  
**Linux:** Path shown in file manager address bar

---

## 💡 Tips

- Create a dedicated music folder before configuring
- Downloaded tracks are saved as `TrackName.mp3`
- Album art is automatically embedded
- Run multiple times to get new tracks from the same playlist
- Check `Data/downloaded_tracks.json` to see what you've downloaded

---

## 📄 License

MIT License - Free to use and modify

---

## 🙏 Credits

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) - Download functionality
- [ffmpeg](https://ffmpeg.org/) - Audio processing
- [Spotify Web API](https://developer.spotify.com/) - Playlist data

---

**Made with ❤️ using C# and ASP.NET Core**
