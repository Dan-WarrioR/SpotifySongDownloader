# 🎵 Music Downloader

Download Spotify playlists and YouTube videos as high-quality MP3s with embedded metadata and album art — all from a local web UI.

![Platform](https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Language](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)

---

## ✨ Features

| Feature | Details |
|---|---|
| 🎵 **Spotify playlists** | Download full playlists by ID — add as many as you want |
| ▶️ **YouTube URLs** | Paste any YouTube link and download it as MP3 |
| 🔍 **Auto-fetch metadata** | One click fills in title and artist from YouTube |
| 🖼️ **Album art** | Embedded automatically into every MP3 (ID3v2.3) |
| ⚡ **Concurrent downloads** | 3 tracks downloaded in parallel for speed |
| ✕ **Cancel anytime** | Stop a running batch without killing the app |
| 🔄 **Smart skip** | Already-downloaded tracks are detected and skipped |
| 📊 **Live progress** | Real-time progress bar, per-track results, counts |
| 📂 **Open folder** | Jump straight to your download folder from the UI |
| 📋 **Download history** | Full log of every track — Spotify and YouTube combined |

---

## 🚀 Quick Start

### Requirements

You need these installed and accessible from PATH (or placed next to the executable):

- **[yt-dlp](https://github.com/yt-dlp/yt-dlp/releases)** — handles all downloading
- **[ffmpeg](https://ffmpeg.org/download.html)** — handles audio conversion and metadata embedding

**Windows one-liner:**
```powershell
winget install yt-dlp && winget install ffmpeg
```

For Spotify downloads you also need a free API app:
1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Create an app (any name, any redirect URI)
3. Copy the **Client ID** and **Client Secret**

---

### Option A — Run from source (requires .NET 10 SDK)

```bash
# Clone or download the repo, then:
run.bat
```

Opens `http://localhost:5000` automatically.

---

### Option B — Portable executable (recommended)

No .NET installation needed on the target machine.

```bash
# From the project folder, run once:
publish.bat
```

This produces a `dist/` folder. Drop in your `yt-dlp.exe` and `ffmpeg.exe`, then:

```
dist/
  SpotifyDownloader.exe    ← self-contained, ~80MB
  wwwroot/                 ← static assets (required, keep alongside exe)
  yt-dlp.exe               ← add this
  ffmpeg.exe               ← add this
  launch.bat               ← double-click to run
```

Copy the entire `dist/` folder anywhere — USB drive, another PC, wherever.

---

## 📖 How to Use

### ⚙️ First-time Setup

1. Open the **Configuration** tab
2. Enter your **Spotify Client ID** and **Client Secret**
3. Add one or more **Playlist IDs** using the `+ Add` button
   - Get the ID from a Spotify playlist URL: `open.spotify.com/playlist/`**`37i9dQZF1DX...`**
   - Playlist names load automatically once credentials are saved
4. Set your **Download Folder** (use the quick buttons or paste a path)
5. Click **💾 Save Configuration**

---

### 🎵 Downloading a Spotify Playlist

1. Go to the **Dashboard** tab
2. Click **⬇️ Download All Playlists**
   - Or click the ⬇️ icon next to a specific playlist to download just that one
3. Watch the live progress — completed and failed tracks are listed as they finish
4. Click **✕ Cancel Download** at any time to stop queuing new tracks
5. Click **📂 Open** to jump to your download folder when done

Already-downloaded tracks are automatically skipped. Run it again after adding songs to a playlist and only the new ones will be fetched.

> **Re-download everything:** Use the *Re-download All Tracks* button at the bottom of the Dashboard to clear history and re-fetch everything from scratch.

---

### ▶️ Downloading from YouTube

1. Go to the **YouTube** tab
2. Paste a YouTube URL
3. Click **🔍 Fetch Info** to auto-fill title and artist from the video
4. Fill in any additional metadata: album, year, cover art URL (all optional)
5. Optionally set a custom download folder for this track
6. Click **▶️ Download from YouTube**

The downloaded file is added to your download history alongside Spotify tracks.

---

## 🗂️ Data & Settings

All persistent data lives in a `Data/` folder next to the executable:

| File | Contents |
|---|---|
| `Data/config.json` | Spotify credentials, playlist IDs, download folder, naming pattern |
| `Data/downloaded_tracks.json` | Full history — track name, artist, file path, source, timestamp |

These files are created automatically on first run. Back them up if you want to preserve history when moving the app.

---

## ❓ Troubleshooting

<details>
<summary><b>App won't start / port in use</b></summary>

Port 5000 is taken by another process. Change the port in `Scripts/Core/Application.cs`:
```csharp
_app.Run("http://localhost:5001");  // pick any free port
```
Then rebuild.
</details>

<details>
<summary><b>Downloads fail immediately</b></summary>

- Run `yt-dlp --version` and `ffmpeg -version` in a terminal to confirm they're reachable
- If using the portable exe, make sure `yt-dlp.exe` and `ffmpeg.exe` are in the same folder as `SpotifyDownloader.exe`
- The Dashboard shows a green/red status indicator for both tools
</details>

<details>
<summary><b>Spotify authentication fails</b></summary>

- Double-check Client ID and Client Secret in the Configuration tab
- Make sure you saved the config after entering credentials
- Credentials are stored in `Data/config.json` — you can inspect it directly
</details>

<details>
<summary><b>YouTube info fetch returns nothing</b></summary>

- The URL must be a direct video link, not a playlist or channel page
- Private or age-restricted videos cannot be fetched
- Update yt-dlp: `yt-dlp -U`
</details>

<details>
<summary><b>Album art not embedded</b></summary>

- ffmpeg is required for art embedding — confirm it's installed
- The art URL must be publicly accessible (Spotify CDN URLs work fine)
- Re-downloading with *Clear History & Re-download All* will re-embed art
</details>

---

## 🏗️ Project Structure

```
SpotifyDownloader/
├── run.bat / run.sh              # Launch scripts (requires .NET SDK)
├── publish.bat                   # Build portable self-contained exe
├── SpotifyDownloader.csproj
├── Data/                         # Created at runtime
│   ├── config.json
│   └── downloaded_tracks.json
├── Pages/
│   └── Index.cshtml              # Single-page UI
├── wwwroot/
│   ├── css/site.css
│   └── js/site.js
└── Scripts/
    ├── Core/                     # Shared utilities (FileHelper, MediaEmbedder, ToolPaths…)
    ├── Controllers/              # API endpoints
    ├── Data/                     # DownloadDatabase
    └── Features/
        ├── Config/               # Configuration management
        ├── Download/             # Spotify download pipeline
        ├── Spotify/              # Spotify API client
        └── YouTube/              # YouTube download pipeline
```

---

## 🔧 Dependencies

| Tool | Purpose |
|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Downloads audio from YouTube and other sites |
| [ffmpeg](https://ffmpeg.org/) | Converts to MP3, embeds metadata and album art |
| [Spotify Web API](https://developer.spotify.com/) | Fetches track lists and metadata from playlists |
| ASP.NET Core 10 | Web server and API |

---

## 📄 License

MIT — free to use, modify, and distribute.