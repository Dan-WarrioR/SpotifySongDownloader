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
        .container { max-width: 900px; margin: 0 auto; padding: 60px 24px; position: relative; z-index: 1; }
        header { text-align: center; margin-bottom: 60px; animation: fadeInDown 0.8s ease-out; }
        h1 {
            font-family: 'Syne', sans-serif; font-size: 4rem; font-weight: 800;
            background: linear-gradient(135deg, var(--accent) 0%, #00d4ff 100%);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent;
            background-clip: text; margin-bottom: 12px; letter-spacing: -2px;
        }
        .subtitle { font-size: 1.2rem; color: var(--text-secondary); font-weight: 300; }
        .card {
            background: var(--bg-card); border: 1px solid var(--border); border-radius: 24px;
            padding: 36px; margin-bottom: 24px; backdrop-filter: blur(20px);
            position: relative; overflow: hidden; transition: all 0.3s ease;
            animation: fadeInUp 0.8s ease-out backwards;
        }
        .card::before {
            content: ''; position: absolute; top: 0; left: 0; right: 0; height: 2px;
            background: linear-gradient(90deg, transparent, var(--accent), transparent);
            opacity: 0; transition: opacity 0.3s;
        }
        .card:hover::before { opacity: 1; }
        .card:nth-child(1) { animation-delay: 0.1s; }
        .card:nth-child(2) { animation-delay: 0.2s; }
        .card:nth-child(3) { animation-delay: 0.3s; }
        .card:nth-child(4) { animation-delay: 0.4s; }
        .card-title {
            font-family: 'Syne', sans-serif; font-size: 1.5rem; font-weight: 600;
            margin-bottom: 24px; display: flex; align-items: center; gap: 12px;
        }
        .card-title::before { content: ''; width: 4px; height: 24px; background: var(--accent); border-radius: 2px; }
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
        .current-value {
            margin-top: 8px; padding: 10px 14px; background: var(--bg-secondary);
            border-radius: 8px; font-size: 0.9rem; color: var(--text-primary);
            border-left: 3px solid var(--border); display: none;
        }
        .current-value.show { display: block; }
        .current-value.filled { 
            border-left-color: var(--success);
            background: rgba(0, 255, 148, 0.05);
        }
        .current-value.empty { 
            border-left-color: var(--warning);
            background: rgba(255, 217, 61, 0.05);
        }
        .current-value::before { 
            font-weight: 600; 
            margin-right: 8px;
        }
        .current-value.filled::before { 
            content: '✓ Saved: '; 
            color: var(--success); 
        }
        .current-value.empty::before { 
            content: '⚠ Not set'; 
            color: var(--warning);
            margin-right: 0;
        }
        .field-status {
            display: inline-block;
            width: 8px;
            height: 8px;
            border-radius: 50%;
            margin-left: 8px;
            vertical-align: middle;
        }
        .field-status.filled { background: var(--success); }
        .field-status.empty { background: var(--warning); }
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
        .btn-primary {
            background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dim) 100%);
            color: var(--bg-primary); box-shadow: 0 8px 24px rgba(0, 255, 148, 0.3);
        }
        .btn-primary:hover { transform: translateY(-2px); box-shadow: 0 12px 32px rgba(0, 255, 148, 0.4); }
        .btn-primary:active { transform: translateY(0); }
        .btn-secondary { background: var(--bg-secondary); color: var(--text-primary); border: 2px solid var(--border); }
        .btn-secondary:hover { border-color: var(--accent); box-shadow: 0 4px 16px rgba(0, 255, 148, 0.2); }
        .btn-download {
            width: 100%; padding: 24px; font-size: 1.2rem;
            background: linear-gradient(135deg, #00ff94 0%, #00d4ff 100%);
            position: relative; overflow: hidden;
        }
        .btn-download::after {
            content: ''; position: absolute; top: 50%; left: 0; width: 0; height: 100%;
            background: rgba(255, 255, 255, 0.2); transform: translateY(-50%) skewX(-20deg);
            transition: width 0.6s; z-index: 0;
        }
        .btn-download:hover::after { width: 200%; }
        .btn-download span { position: relative; z-index: 1; }
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
        .status { padding: 12px 20px; border-radius: 8px; margin-bottom: 16px; font-weight: 600; }
        .status.success { background: rgba(0, 255, 148, 0.1); color: var(--success); border: 1px solid var(--success); }
        .status.error { background: rgba(255, 77, 109, 0.1); color: var(--danger); border: 1px solid var(--danger); }
        .status.warning { background: rgba(255, 217, 61, 0.1); color: var(--warning); border: 1px solid var(--warning); }
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
        .config-status-summary {
            margin-bottom: 24px; padding: 16px; background: var(--bg-secondary);
            border-radius: 12px; border: 2px solid var(--border);
            display: flex; align-items: center; gap: 16px; flex-wrap: wrap;
        }
        .config-status-summary.complete {
            border-color: var(--success);
            background: rgba(0, 255, 148, 0.05);
        }
        .config-status-summary.incomplete {
            border-color: var(--warning);
            background: rgba(255, 217, 61, 0.05);
        }
        .config-status-icon {
            font-size: 2rem;
        }
        .config-status-text {
            flex: 1;
        }
        .config-status-title {
            font-weight: 700;
            font-size: 1rem;
            margin-bottom: 4px;
        }
        .config-status-detail {
            font-size: 0.85rem;
            color: var(--text-secondary);
        }
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
        @keyframes fadeInDown {
            from { opacity: 0; transform: translateY(-30px); }
            to { opacity: 1; transform: translateY(0); }
        }
        @keyframes fadeInUp {
            from { opacity: 0; transform: translateY(30px); }
            to { opacity: 1; transform: translateY(0); }
        }
        @media (max-width: 768px) {
            h1 { font-size: 2.5rem; }
            .stats { grid-template-columns: repeat(3, 1fr); }
            .card { padding: 24px; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <header>
            <h1>🎵 Spotify</h1>
            <p class=""subtitle"">Download your favorite playlists</p>
        </header>

        <div class=""card"">
            <h2 class=""card-title"">Configuration</h2>
            
            <div class=""config-status-summary incomplete"" id=""configStatusSummary"">
                <div class=""config-status-icon"">⚙️</div>
                <div class=""config-status-text"">
                    <div class=""config-status-title"" id=""configStatusTitle"">Configuration Incomplete</div>
                    <div class=""config-status-detail"" id=""configStatusDetail"">Please fill in all required fields</div>
                </div>
            </div>
            
            <div class=""tools-check"" id=""toolsCheck"">
                <div class=""tool-status"" id=""ytdlpStatus"">
                    <span>yt-dlp</span>
                </div>
                <div class=""tool-status"" id=""ffmpegStatus"">
                    <span>ffmpeg</span>
                </div>
            </div>
            <form id=""configForm"">
                <div class=""form-group"">
                    <label for=""clientId"">Spotify Client ID<span class=""field-status empty"" id=""statusClientId""></span></label>
                    <input type=""text"" id=""clientId"" placeholder=""Enter your Spotify Client ID"">
                    <div class=""current-value empty show"" id=""currentClientId""></div>
                </div>
                <div class=""form-group"">
                    <label for=""clientSecret"">Spotify Client Secret<span class=""field-status empty"" id=""statusClientSecret""></span></label>
                    <input type=""password"" id=""clientSecret"" placeholder=""Enter your Spotify Client Secret"">
                    <div class=""current-value empty show"" id=""currentClientSecret""></div>
                </div>
                <div class=""form-group"">
                    <label for=""playlistId"">Playlist ID<span class=""field-status empty"" id=""statusPlaylistId""></span></label>
                    <input type=""text"" id=""playlistId"" placeholder=""Enter Spotify Playlist ID"">
                    <div class=""current-value empty show"" id=""currentPlaylistId""></div>
                </div>
                <div class=""form-group"">
                    <label for=""downloadFolder"">Download Folder<span class=""field-status empty"" id=""statusDownloadFolder""></span></label>
                    <input type=""text"" id=""downloadFolder"" placeholder=""e.g., C:\Music\Spotify or /home/user/Music"">
                    <div class=""current-value empty show"" id=""currentDownloadFolder""></div>
                    <div style=""margin-top: 8px; font-size: 0.85rem; color: var(--text-secondary); font-style: italic;"">
                        💡 Copy the full path from your file manager
                    </div>
                </div>
                <button type=""submit"" class=""btn-primary"">Save Configuration</button>
            </form>
        </div>

        <div class=""card"">
            <h2 class=""card-title"">Download</h2>
            
            <div id=""currentConfigDisplay"" style=""margin-bottom: 20px; padding: 16px; background: var(--bg-secondary); border-radius: 12px; border: 1px solid var(--border); display: none;"">
                <div style=""font-weight: 600; margin-bottom: 12px; color: var(--accent);"">Current Configuration:</div>
                <div style=""font-size: 0.9rem; color: var(--text-secondary); line-height: 1.8;"">
                    <div><strong>Playlist:</strong> <span id=""displayPlaylistId"">-</span></div>
                    <div><strong>Download to:</strong> <span id=""displayDownloadFolder"">-</span></div>
                </div>
            </div>
            
            <div id=""statusMessage""></div>
            <button id=""downloadBtn"" class=""btn-download""><span>Start Download</span></button>
            
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
        </div>
    </div>

    <script>
        async function loadConfig() {
            const response = await fetch('/api/config');
            const config = await response.json();
            
            document.getElementById('clientId').value = config.client_id || '';
            document.getElementById('clientSecret').value = config.client_secret || '';
            document.getElementById('playlistId').value = config.playlist_id || '';
            document.getElementById('downloadFolder').value = config.download_folder || '';
            
            updateFieldStatus('clientId', config.client_id);
            updateFieldStatus('clientSecret', config.client_secret, true);
            updateFieldStatus('playlistId', config.playlist_id);
            updateFieldStatus('downloadFolder', config.download_folder);
            
            const configDisplay = document.getElementById('currentConfigDisplay');
            if (config.playlist_id && config.download_folder) {
                document.getElementById('displayPlaylistId').textContent = config.playlist_id;
                document.getElementById('displayDownloadFolder').textContent = config.download_folder;
                configDisplay.style.display = 'block';
            } else {
                configDisplay.style.display = 'none';
            }
        }

        function updateFieldStatus(fieldName, value, isSecret = false) {
            const currentEl = document.getElementById('current' + fieldName.charAt(0).toUpperCase() + fieldName.slice(1));
            const statusEl = document.getElementById('status' + fieldName.charAt(0).toUpperCase() + fieldName.slice(1));
            
            if (value && value.trim() !== '') {
                currentEl.classList.remove('empty');
                currentEl.classList.add('filled');
                statusEl.classList.remove('empty');
                statusEl.classList.add('filled');
                
                if (isSecret) {
                    currentEl.textContent = '•'.repeat(Math.min(value.length, 20));
                } else {
                    currentEl.textContent = value;
                }
            } else {
                currentEl.classList.remove('filled');
                currentEl.classList.add('empty');
                statusEl.classList.remove('filled');
                statusEl.classList.add('empty');
                currentEl.textContent = '';
            }
            
            updateOverallStatus();
        }

        function updateOverallStatus() {
            const clientId = document.getElementById('clientId').value.trim();
            const clientSecret = document.getElementById('clientSecret').value.trim();
            const playlistId = document.getElementById('playlistId').value.trim();
            const downloadFolder = document.getElementById('downloadFolder').value.trim();
            
            const allFilled = clientId && clientSecret && playlistId && downloadFolder;
            const summary = document.getElementById('configStatusSummary');
            const title = document.getElementById('configStatusTitle');
            const detail = document.getElementById('configStatusDetail');
            
            if (allFilled) {
                summary.classList.remove('incomplete');
                summary.classList.add('complete');
                title.textContent = '✓ Configuration Complete';
                detail.textContent = 'All fields are configured. Ready to download!';
            } else {
                summary.classList.remove('complete');
                summary.classList.add('incomplete');
                title.textContent = '⚠ Configuration Incomplete';
                
                const missing = [];
                if (!clientId) missing.push('Client ID');
                if (!clientSecret) missing.push('Client Secret');
                if (!playlistId) missing.push('Playlist ID');
                if (!downloadFolder) missing.push('Download Folder');
                
                detail.textContent = 'Missing: ' + missing.join(', ');
            }
        }

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
            
            showStatus('Configuration saved!', 'success');
            loadConfig();
        });

        document.getElementById('downloadBtn').addEventListener('click', async () => {
            const response = await fetch('/api/download', { method: 'POST' });
            const data = await response.json();
            
            if (response.ok) {
                if (data.new === 0) {
                    showStatus(data.message, 'warning');
                } else {
                    showStatus(data.message, 'success');
                    document.getElementById('progressSection').classList.add('active');
                    startProgressPolling();
                }
            } else {
                showStatus(data.error, 'error');
            }
        });

        function showStatus(message, type) {
            const statusEl = document.getElementById('statusMessage');
            statusEl.className = 'status ' + type;
            statusEl.textContent = message;
            setTimeout(() => statusEl.textContent = '', 5000);
        }

        let pollingInterval;
        function startProgressPolling() {
            pollingInterval = setInterval(async () => {
                const response = await fetch('/api/download/progress');
                const state = await response.json();
                
                updateProgress(state);
                
                if (!state.in_progress && state.total > 0) {
                    clearInterval(pollingInterval);
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

        loadConfig();
        checkTools();
    </script>
</body>
</html>";
    }
}
