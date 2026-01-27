namespace SpotifyDownloader.Scripts.Core
{
    public static class HtmlTemplate
    {
        public const string Content = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Spotify Downloader</title>
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700;900&family=Syne:wght@400;600;800&display=swap"" rel=""stylesheet"">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        :root {
            --bg-primary: #0a0e14; --bg-secondary: #12171f; --bg-card: #1a2332;
            --accent: #00ff94; --accent-dim: #00cc76;
            --text-primary: #e8eef5; --text-secondary: #8b95a5;
            --danger: #ff4d6d; --warning: #ffd93d; --success: #00ff94;
            --border: rgba(255, 255, 255, 0.08);
        }
        body {
            font-family: 'Outfit', sans-serif; background: var(--bg-primary);
            color: var(--text-primary); min-height: 100vh; overflow-x: hidden; position: relative;
        }
        body::before {
            content: ''; position: fixed; top: -50%; left: -50%; width: 200%; height: 200%;
            background: radial-gradient(circle at 20% 30%, rgba(0, 255, 148, 0.15) 0%, transparent 50%),
                        radial-gradient(circle at 80% 70%, rgba(138, 43, 226, 0.1) 0%, transparent 50%),
                        radial-gradient(circle at 50% 50%, rgba(255, 77, 109, 0.08) 0%, transparent 50%);
            animation: drift 20s ease-in-out infinite; pointer-events: none; z-index: 0;
        }
        @keyframes drift {
            0%, 100% { transform: translate(0, 0) rotate(0deg); }
            33% { transform: translate(5%, -5%) rotate(5deg); }
            66% { transform: translate(-5%, 5%) rotate(-5deg); }
        }
        .container { max-width: 1000px; margin: 0 auto; padding: 60px 24px; position: relative; z-index: 1; }
        header { text-align: center; margin-bottom: 40px; animation: fadeInDown 0.8s ease-out; }
        h1 {
            font-family: 'Syne', sans-serif; font-size: 3.5rem; font-weight: 800;
            background: linear-gradient(135deg, var(--accent) 0%, #00d4ff 100%);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent;
            background-clip: text; margin-bottom: 12px; letter-spacing: -2px;
        }
        .subtitle { font-size: 1.1rem; color: var(--text-secondary); font-weight: 300; }
        
        /* Tabs */
        .tabs {
            display: flex; gap: 8px; margin-bottom: 24px;
            background: var(--bg-secondary); padding: 8px; border-radius: 16px;
            border: 1px solid var(--border);
        }
        .tab {
            flex: 1; padding: 16px 24px; background: transparent; border: none;
            color: var(--text-secondary); font-family: 'Outfit', sans-serif;
            font-size: 1rem; font-weight: 600; cursor: pointer;
            border-radius: 12px; transition: all 0.3s ease;
            text-transform: uppercase; letter-spacing: 1px;
        }
        .tab:hover { background: rgba(0, 255, 148, 0.05); color: var(--text-primary); }
        .tab.active {
            background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dim) 100%);
            color: var(--bg-primary); box-shadow: 0 4px 16px rgba(0, 255, 148, 0.3);
        }
        
        .tab-content { display: none; animation: fadeIn 0.5s ease-out; }
        .tab-content.active { display: block; }
        
        .card {
            background: var(--bg-card); border: 1px solid var(--border); border-radius: 24px;
            padding: 36px; margin-bottom: 24px; backdrop-filter: blur(20px);
            position: relative; overflow: hidden; transition: all 0.3s ease;
        }
        .card::before {
            content: ''; position: absolute; top: 0; left: 0; right: 0; height: 2px;
            background: linear-gradient(90deg, transparent, var(--accent), transparent);
            opacity: 0; transition: opacity 0.3s;
        }
        .card:hover::before { opacity: 1; }
        
        .card-title {
            font-family: 'Syne', sans-serif; font-size: 1.5rem; font-weight: 600;
            margin-bottom: 24px; display: flex; align-items: center; gap: 12px;
        }
        .card-title::before { content: ''; width: 4px; height: 24px; background: var(--accent); border-radius: 2px; }
        
        /* Info Grid */
        .info-grid {
            display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 16px; margin-bottom: 24px;
        }
        .info-box {
            background: var(--bg-secondary); padding: 20px; border-radius: 16px;
            border: 1px solid var(--border); text-align: center;
        }
        .info-label {
            font-size: 0.8rem; color: var(--text-secondary);
            text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px;
        }
        .info-value {
            font-size: 1.8rem; font-weight: 700;
            background: linear-gradient(135deg, var(--accent) 0%, #00d4ff 100%);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent;
            background-clip: text;
        }
        .info-value.text {
            font-size: 1rem; font-weight: 600; color: var(--text-primary);
            word-break: break-word; background: none;
            -webkit-background-clip: unset; -webkit-text-fill-color: unset;
        }
        .info-value.empty {
            color: var(--text-secondary); opacity: 0.5;
            background: none; -webkit-background-clip: unset; -webkit-text-fill-color: unset;
        }
        
        /* Tools Check */
        .tools-check {
            display: flex; gap: 12px; flex-wrap: wrap; align-items: center;
            margin-bottom: 24px; padding: 16px; background: var(--bg-secondary);
            border-radius: 12px; border: 1px solid var(--border);
        }
        .tool-status {
            display: flex; align-items: center; gap: 8px; padding: 8px 16px;
            background: var(--bg-card); border-radius: 8px; font-size: 0.9rem;
        }
        .tool-status.installed { color: var(--success); }
        .tool-status.missing { color: var(--danger); }
        .tool-status::before { content: '●'; font-size: 1.2rem; }
        
        /* Form Elements */
        .form-group { margin-bottom: 24px; }
        label {
            display: block; margin-bottom: 8px; font-weight: 600; font-size: 0.9rem;
            text-transform: uppercase; letter-spacing: 1px; color: var(--text-secondary);
        }
        input[type=""text""], input[type=""password""] {
            width: 100%; padding: 16px 20px; background: var(--bg-secondary);
            border: 2px solid var(--border); border-radius: 12px;
            color: var(--text-primary); font-size: 1rem; font-family: 'Outfit', sans-serif;
            transition: all 0.3s ease; outline: none;
        }
        input:focus { border-color: var(--accent); box-shadow: 0 0 0 4px rgba(0, 255, 148, 0.1); }
        input::placeholder { color: var(--text-secondary); opacity: 0.5; }
        
        .input-group {
            display: flex; gap: 8px; align-items: stretch;
        }
        .input-group input { flex: 1; }
        .input-group button {
            padding: 12px 20px; white-space: nowrap;
        }
        
        /* Buttons */
        button {
            padding: 16px 32px; border: none; border-radius: 12px;
            font-family: 'Outfit', sans-serif; font-size: 1rem; font-weight: 600;
            cursor: pointer; transition: all 0.3s ease; text-transform: uppercase;
            letter-spacing: 1px; position: relative; overflow: hidden;
        }
        button::before {
            content: ''; position: absolute; top: 50%; left: 50%; width: 0; height: 0;
            border-radius: 50%; background: rgba(255, 255, 255, 0.3);
            transform: translate(-50%, -50%); transition: width 0.6s, height 0.6s;
        }
        button:active::before { width: 300px; height: 300px; }
        button:disabled {
            opacity: 0.5; cursor: not-allowed;
        }
        
        .btn-primary {
            background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dim) 100%);
            color: var(--bg-primary); box-shadow: 0 8px 24px rgba(0, 255, 148, 0.3);
        }
        .btn-primary:hover:not(:disabled) { transform: translateY(-2px); box-shadow: 0 12px 32px rgba(0, 255, 148, 0.4); }
        .btn-primary:active { transform: translateY(0); }
        
        .btn-secondary { 
            background: var(--bg-secondary); color: var(--text-primary); 
            border: 2px solid var(--border); box-shadow: none;
        }
        .btn-secondary:hover { border-color: var(--accent); box-shadow: 0 4px 16px rgba(0, 255, 148, 0.2); }
        
        .btn-danger {
            background: linear-gradient(135deg, var(--danger) 0%, #cc3d57 100%);
            color: white; box-shadow: 0 8px 24px rgba(255, 77, 109, 0.3);
        }
        .btn-danger:hover:not(:disabled) { transform: translateY(-2px); box-shadow: 0 12px 32px rgba(255, 77, 109, 0.4); }
        
        .btn-download {
            width: 100%; padding: 24px; font-size: 1.2rem;
            background: linear-gradient(135deg, #00ff94 0%, #00d4ff 100%);
        }
        .btn-download::after {
            content: ''; position: absolute; top: 50%; left: 0; width: 0; height: 100%;
            background: rgba(255, 255, 255, 0.2); transform: translateY(-50%) skewX(-20deg);
            transition: width 0.6s; z-index: 0;
        }
        .btn-download:hover::after { width: 200%; }
        .btn-download span { position: relative; z-index: 1; }
        
        /* Progress */
        .progress-section { display: none; margin-top: 24px; }
        .progress-section.active { display: block; }
        .progress-bar-container {
            width: 100%; height: 12px; background: var(--bg-secondary);
            border-radius: 8px; overflow: hidden; margin-bottom: 16px; position: relative;
        }
        .progress-bar {
            height: 100%; background: linear-gradient(90deg, var(--accent) 0%, #00d4ff 100%);
            transition: width 0.3s ease; position: relative;
        }
        .progress-bar::after {
            content: ''; position: absolute; top: 0; left: 0; right: 0; bottom: 0;
            background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent);
            animation: shimmer 2s infinite;
        }
        @keyframes shimmer {
            0% { transform: translateX(-100%); }
            100% { transform: translateX(100%); }
        }
        .progress-text { text-align: center; color: var(--text-secondary); margin-bottom: 12px; }
        .current-track {
            text-align: center; font-size: 1.1rem; color: var(--accent);
            margin-bottom: 16px; font-weight: 600; min-height: 28px;
        }
        .stats {
            display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin-top: 16px;
        }
        .stat {
            background: var(--bg-secondary); padding: 16px; border-radius: 12px;
            text-align: center; border: 1px solid var(--border);
        }
        .stat-value {
            font-size: 2rem; font-weight: 700; background: linear-gradient(135deg, var(--accent) 0%, #00d4ff 100%);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent;
            background-clip: text;
        }
        .stat-label { font-size: 0.8rem; color: var(--text-secondary); margin-top: 4px; text-transform: uppercase; letter-spacing: 1px; }
        
        /* Status Messages */
        .status { padding: 12px 20px; border-radius: 8px; margin-bottom: 16px; font-weight: 600; }
        .status.success { background: rgba(0, 255, 148, 0.1); color: var(--success); border: 1px solid var(--success); }
        .status.error { background: rgba(255, 77, 109, 0.1); color: var(--danger); border: 1px solid var(--danger); }
        .status.warning { background: rgba(255, 217, 61, 0.1); color: var(--warning); border: 1px solid var(--warning); }
        
        /* Results List */
        .results-list {
            max-height: 300px; overflow-y: auto; margin-top: 16px;
            background: var(--bg-secondary); border-radius: 12px; padding: 16px;
        }
        .result-item {
            padding: 12px; margin-bottom: 8px; border-radius: 8px;
            background: var(--bg-card); border-left: 3px solid;
        }
        .result-item.success { border-color: var(--success); }
        .result-item.error { border-color: var(--danger); }
        .result-track { font-weight: 600; margin-bottom: 4px; }
        .result-artist { font-size: 0.9rem; color: var(--text-secondary); }
        .result-error { font-size: 0.85rem; color: var(--danger); margin-top: 4px; }
        
        /* Danger Zone */
        .danger-zone {
            margin-top: 32px; padding: 24px; background: rgba(255, 77, 109, 0.05);
            border: 2px solid rgba(255, 77, 109, 0.2); border-radius: 16px;
        }
        .danger-zone h3 {
            color: var(--danger); margin-bottom: 12px; font-size: 1.2rem;
        }
        .danger-zone p {
            color: var(--text-secondary); margin-bottom: 16px; line-height: 1.6;
        }
        
        /* Animations */
        @keyframes fadeInDown {
            from { opacity: 0; transform: translateY(-30px); }
            to { opacity: 1; transform: translateY(0); }
        }
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        
        @media (max-width: 768px) {
            h1 { font-size: 2.5rem; }
            .info-grid { grid-template-columns: 1fr; }
            .stats { grid-template-columns: repeat(3, 1fr); }
            .card { padding: 24px; }
            .tabs { flex-direction: column; }
            .tab { padding: 12px; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <header>
            <h1>🎵 Spotify Downloader</h1>
            <p class=""subtitle"">Download your favorite playlists with album art</p>
        </header>

        <div class=""tabs"">
            <button class=""tab active"" onclick=""switchTab('dashboard')"">📊 Dashboard</button>
            <button class=""tab"" onclick=""switchTab('config')"">⚙️ Configuration</button>
        </div>

        <!-- DASHBOARD TAB -->
        <div id=""dashboard-tab"" class=""tab-content active"">
            <div class=""card"">
                <h2 class=""card-title"">Current Setup</h2>
                
                <div class=""info-grid"">
                    <div class=""info-box"">
                        <div class=""info-label"">Playlist ID</div>
                        <div class=""info-value text empty"" id=""infoPlaylistId"">Not configured</div>
                    </div>
                    <div class=""info-box"">
                        <div class=""info-label"">Download Folder</div>
                        <div class=""info-value text empty"" id=""infoDownloadFolder"">Not configured</div>
                    </div>
                    <div class=""info-box"">
                        <div class=""info-label"">Total Downloaded</div>
                        <div class=""info-value"" id=""infoTotalDownloaded"">0</div>
                    </div>
                </div>
                
                <div class=""tools-check"" id=""toolsCheck"">
                    <div style=""flex: 1; font-weight: 600; color: var(--text-secondary);"">Required Tools:</div>
                    <div class=""tool-status"" id=""ytdlpStatus"">
                        <span>yt-dlp</span>
                    </div>
                    <div class=""tool-status"" id=""ffmpegStatus"">
                        <span>ffmpeg</span>
                    </div>
                </div>
            </div>

            <div class=""card"">
                <h2 class=""card-title"">Download Playlist</h2>
                
                <div id=""statusMessage""></div>
                <button id=""downloadBtn"" class=""btn-download"" disabled><span>⬇️ Start Download</span></button>
                
                <div id=""progressSection"" class=""progress-section"">
                    <div class=""current-track"" id=""currentTrack""></div>
                    <div class=""progress-text"" id=""progressText"">0%</div>
                    <div class=""progress-bar-container"">
                        <div class=""progress-bar"" id=""progressBar""></div>
                    </div>
                    <div class=""stats"">
                        <div class=""stat"">
                            <div class=""stat-value"" id=""completedCount"">0</div>
                            <div class=""stat-label"">Completed</div>
                        </div>
                        <div class=""stat"">
                            <div class=""stat-value"" id=""failedCount"">0</div>
                            <div class=""stat-label"">Failed</div>
                        </div>
                        <div class=""stat"">
                            <div class=""stat-value"" id=""totalCount"">0</div>
                            <div class=""stat-label"">Total</div>
                        </div>
                    </div>
                    <div id=""resultsList"" class=""results-list""></div>
                </div>
                
                <div class=""danger-zone"">
                    <h3>⚠️ Re-download All Tracks</h3>
                    <p>
                        This will clear your download history and re-download all tracks from the playlist. 
                        Use this if you have corrupted files or missing album art.
                    </p>
                    <button id=""clearHistoryBtn"" class=""btn-danger"">🔄 Clear History & Re-download All</button>
                </div>
            </div>
        </div>

        <!-- CONFIGURATION TAB -->
        <div id=""config-tab"" class=""tab-content"">
            <div class=""card"">
                <h2 class=""card-title"">Spotify API Configuration</h2>
                
                <form id=""configForm"">
                    <div class=""form-group"">
                        <label for=""clientId"">Spotify Client ID</label>
                        <input type=""text"" id=""clientId"" placeholder=""Enter your Spotify Client ID"" required>
                        <div style=""margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary);"">
                            Get this from <a href=""https://developer.spotify.com/dashboard"" target=""_blank"" style=""color: var(--accent);"">Spotify Developer Dashboard</a>
                        </div>
                    </div>
                    
                    <div class=""form-group"">
                        <label for=""clientSecret"">Spotify Client Secret</label>
                        <input type=""text"" id=""clientSecret"" placeholder=""Enter your Spotify Client Secret"" required>
                    </div>
                    
                    <div class=""form-group"">
                        <label for=""playlistId"">Playlist ID</label>
                        <input type=""text"" id=""playlistId"" placeholder=""e.g., 37i9dQZF1DXcBWIGoYBM5M"" required>
                        <div style=""margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary);"">
                            💡 Copy from playlist URL: spotify.com/playlist/<strong>PLAYLIST_ID</strong>
                        </div>
                    </div>
                    
                    <div class=""form-group"">
                        <label for=""downloadFolder"">Download Folder</label>
                        <div class=""input-group"">
                            <input type=""text"" id=""downloadFolder"" placeholder=""Path to download folder"" required>
                            <button type=""button"" class=""btn-secondary"" onclick=""setDefaultMusicFolder()"">🎵 Music</button>
                            <button type=""button"" class=""btn-secondary"" onclick=""setDefaultDownloadsFolder()"">📥 Downloads</button>
                        </div>
                        <div style=""margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary); font-style: italic;"">
                            Quick buttons will set common default paths for your OS
                        </div>
                    </div>
                    
                    <button type=""submit"" class=""btn-primary"" style=""width: 100%;"">💾 Save Configuration</button>
                </form>
            </div>
            
            <div class=""card"">
                <h2 class=""card-title"">Current Configuration</h2>
                
                <div class=""info-grid"">
                    <div class=""info-box"">
                        <div class=""info-label"">Client ID</div>
                        <div class=""info-value text empty"" id=""displayClientId"">Not set</div>
                    </div>
                    <div class=""info-box"">
                        <div class=""info-label"">Client Secret</div>
                        <div class=""info-value text empty"" id=""displayClientSecret"">Not set</div>
                    </div>
                    <div class=""info-box"">
                        <div class=""info-label"">Playlist ID</div>
                        <div class=""info-value text empty"" id=""displayPlaylistId"">Not set</div>
                    </div>
                    <div class=""info-box"">
                        <div class=""info-label"">Download Folder</div>
                        <div class=""info-value text empty"" id=""displayDownloadFolder"">Not set</div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let currentConfig = {};
        
        // Tab Switching
        function switchTab(tabName) {
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));
            
            event.target.classList.add('active');
            document.getElementById(tabName + '-tab').classList.add('active');
        }
        
        // Load Configuration
        async function loadConfig() {
            const response = await fetch('/api/config');
            currentConfig = await response.json();
            
            // Update form inputs
            document.getElementById('clientId').value = currentConfig.client_id || '';
            document.getElementById('clientSecret').value = currentConfig.client_secret || '';
            document.getElementById('playlistId').value = currentConfig.playlist_id || '';
            document.getElementById('downloadFolder').value = currentConfig.download_folder || '';
            
            // Update display values in config tab
            updateDisplayValue('displayClientId', currentConfig.client_id);
            updateDisplayValue('displayClientSecret', currentConfig.client_secret);
            updateDisplayValue('displayPlaylistId', currentConfig.playlist_id);
            updateDisplayValue('displayDownloadFolder', currentConfig.download_folder);
            
            // Update dashboard info
            updateDisplayValue('infoPlaylistId', currentConfig.playlist_id);
            updateDisplayValue('infoDownloadFolder', currentConfig.download_folder);
            
            // Enable download button if config is complete
            updateDownloadButton();
        }
        
        function updateDisplayValue(elementId, value) {
            const el = document.getElementById(elementId);
            if (value && value.trim() !== '') {
                el.textContent = value;
                el.classList.remove('empty');
            } else {
                el.textContent = elementId.includes('info') ? 'Not configured' : 'Not set';
                el.classList.add('empty');
            }
        }
        
        function updateDownloadButton() {
            const btn = document.getElementById('downloadBtn');
            const isConfigured = currentConfig.client_id && currentConfig.client_secret && 
                                 currentConfig.playlist_id && currentConfig.download_folder;
            btn.disabled = !isConfigured;
            
            if (!isConfigured) {
                btn.innerHTML = '<span>⚙️ Configure Settings First</span>';
            } else {
                btn.innerHTML = '<span>⬇️ Start Download</span>';
            }
        }
        
        // Check Tools
        async function checkTools() {
            const [ytdlp, ffmpeg] = await Promise.all([
                fetch('/api/check-ytdlp').then(r => r.json()),
                fetch('/api/check-ffmpeg').then(r => r.json())
            ]);
            
            const ytdlpEl = document.getElementById('ytdlpStatus');
            ytdlpEl.className = 'tool-status ' + (ytdlp.installed ? 'installed' : 'missing');
            
            const ffmpegEl = document.getElementById('ffmpegStatus');
            ffmpegEl.className = 'tool-status ' + (ffmpeg.installed ? 'installed' : 'missing');
        }
        
        // Load Stats
        async function loadStats() {
            try {
                const response = await fetch('/api/stats');
                const stats = await response.json();
                document.getElementById('infoTotalDownloaded').textContent = stats.total_downloaded || 0;
            } catch (e) {
                console.error('Failed to load stats:', e);
            }
        }
        
        // Default Folder Helpers
        function setDefaultMusicFolder() {
            const isWindows = navigator.platform.toLowerCase().includes('win');
            const path = isWindows 
                ? 'C:\\\\Users\\\\' + (localStorage.getItem('username') || 'YourUsername') + '\\\\Music\\\\SpotifyDownloads'
                : '/home/' + (localStorage.getItem('username') || 'user') + '/Music/SpotifyDownloads';
            document.getElementById('downloadFolder').value = path;
        }
        
        function setDefaultDownloadsFolder() {
            const isWindows = navigator.platform.toLowerCase().includes('win');
            const path = isWindows 
                ? 'C:\\\\Users\\\\' + (localStorage.getItem('username') || 'YourUsername') + '\\\\Downloads\\\\SpotifyDownloads'
                : '/home/' + (localStorage.getItem('username') || 'user') + '/Downloads/SpotifyDownloads';
            document.getElementById('downloadFolder').value = path;
        }
        
        // Save Configuration
        document.getElementById('configForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            const config = {
                client_id: document.getElementById('clientId').value,
                client_secret: document.getElementById('clientSecret').value,
                playlist_id: document.getElementById('playlistId').value,
                download_folder: document.getElementById('downloadFolder').value
            };
            
            await fetch('/api/config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });
            
            showStatus('✓ Configuration saved successfully!', 'success');
            await loadConfig();
            await loadStats();
        });
        
        // Start Download
        document.getElementById('downloadBtn').addEventListener('click', async () => {
            if (document.getElementById('downloadBtn').disabled) return;
            
            const response = await fetch('/api/download', { method: 'POST' });
            const data = await response.json();
            
            if (response.ok) {
                if (data.new === 0) {
                    showStatus('ℹ️ ' + data.message, 'warning');
                } else {
                    showStatus('✓ ' + data.message, 'success');
                    document.getElementById('progressSection').classList.add('active');
                    startProgressPolling();
                }
            } else {
                showStatus('✗ ' + data.error, 'error');
            }
        });
        
        // Clear History & Re-download
        document.getElementById('clearHistoryBtn').addEventListener('click', async () => {
            if (!confirm('⚠️ This will clear all download history and re-download ALL tracks from the playlist.\\n\\nAre you sure you want to continue?')) {
                return;
            }
            
            try {
                const response = await fetch('/api/clear-history', { method: 'POST' });
                const data = await response.json();
                
                if (response.ok) {
                    showStatus('✓ Download history cleared! You can now re-download all tracks.', 'success');
                    await loadStats();
                } else {
                    showStatus('✗ Failed to clear history: ' + data.error, 'error');
                }
            } catch (e) {
                showStatus('✗ Error: ' + e.message, 'error');
            }
        });
        
        // Status Messages
        function showStatus(message, type) {
            const statusEl = document.getElementById('statusMessage');
            statusEl.className = 'status ' + type;
            statusEl.textContent = message;
            setTimeout(() => statusEl.textContent = '', 5000);
        }
        
        // Progress Polling
        let pollingInterval;
        function startProgressPolling() {
            pollingInterval = setInterval(async () => {
                const response = await fetch('/api/download/progress');
                const state = await response.json();
                
                updateProgress(state);
                
                if (!state.in_progress && state.total > 0) {
                    clearInterval(pollingInterval);
                    await loadStats();
                }
            }, 500);
        }
        
        function updateProgress(state) {
            document.getElementById('currentTrack').textContent = state.current_track || '';
            document.getElementById('progressText').textContent = state.progress + '%';
            document.getElementById('progressBar').style.width = state.progress + '%';
            document.getElementById('completedCount').textContent = state.completed;
            document.getElementById('failedCount').textContent = state.failed;
            document.getElementById('totalCount').textContent = state.total;
            
            const resultsList = document.getElementById('resultsList');
            resultsList.innerHTML = state.results.map(r => `
                <div class=""result-item ${r.success ? 'success' : 'error'}"">
                    <div class=""result-track"">${r.track}</div>
                    <div class=""result-artist"">${r.artist}</div>
                    ${r.error ? `<div class=""result-error"">${r.error}</div>` : ''}
                </div>
            `).reverse().join('');
        }
        
        // Initialize
        loadConfig();
        checkTools();
        loadStats();
    </script>
</body>
</html>";
    }
}