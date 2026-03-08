// Global state
let currentConfig = {};
let playlists = [];

// ===== TAB SWITCHING =====
function switchTab(tabName, event) {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));

    event.target.classList.add('active');
    document.getElementById(tabName + '-tab').classList.add('active');
}

// ===== CONFIGURATION MANAGEMENT =====
async function loadConfig() {
    const response = await fetch('/api/config');
    currentConfig = await response.json();

    document.getElementById('clientId').value = currentConfig.client_id || '';
    document.getElementById('clientSecret').value = currentConfig.client_secret || '';
    document.getElementById('downloadFolder').value = currentConfig.download_folder || '';
    document.getElementById('fileNamePattern').value = currentConfig.file_name_pattern || 'track';
    document.getElementById('sponsorblock').checked = !!currentConfig.sponsorblock;
    document.getElementById('normalizeVolume').checked = !!currentConfig.normalize_volume;
    document.getElementById('autoSync').checked = !!currentConfig.auto_sync;

    playlists = currentConfig.playlist_ids || [];
    renderPlaylistList();

    updateDisplayValue('displayClientId', currentConfig.client_id);
    updateDisplayValue('displayClientSecret', currentConfig.client_secret);
    updateDisplayValue('displayDownloadFolder', currentConfig.download_folder);
    updateDisplayValue('displayFileNamePattern', getFileNamePatternLabel(currentConfig.file_name_pattern));

    updateDisplayValue('infoDownloadFolder', currentConfig.download_folder);
    updatePlaylistDisplay();
    updateFileNamePreview();
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
        playlists.length > 0 && currentConfig.download_folder;
    btn.disabled = !isConfigured;

    if (!isConfigured) {
        btn.innerHTML = '<span>⚙️ Configure Settings First</span>';
    } else {
        btn.innerHTML = '<span>⬇️ Download All Playlists (' + playlists.length + ')</span>';
    }
}

// ===== PLAYLIST MANAGEMENT =====
function addPlaylist() {
    const input = document.getElementById('newPlaylistId');
    const playlistId = input.value.trim();

    if (!playlistId) {
        showStatus('⚠️ Please enter a playlist ID', 'warning');
        return;
    }

    if (playlists.includes(playlistId)) {
        showStatus('⚠️ Playlist already added', 'warning');
        return;
    }

    playlists.push(playlistId);
    input.value = '';
    renderPlaylistList();
    updatePlaylistDisplay();
}

function removePlaylist(playlistId) {
    playlists = playlists.filter(id => id !== playlistId);
    renderPlaylistList();
    updatePlaylistDisplay();
}

function renderPlaylistList() {
    const container = document.getElementById('playlistList');

    if (playlists.length === 0) {
        container.innerHTML = '<div style="text-align: center; color: var(--text-secondary); padding: 20px;">No playlists added yet</div>';
        return;
    }

    container.innerHTML = playlists.map((id, index) => `
        <div class="playlist-item" data-playlist-id="${id}">
            <div class="playlist-info">
                <div class="playlist-number">${index + 1}</div>
                <div>
                    <div class="playlist-id">${id}</div>
                    <div class="playlist-name" style="font-size: 0.8rem; color: var(--text-secondary); margin-top: 2px;"></div>
                </div>
            </div>
            <div class="playlist-actions">
                <button type="button" class="btn-icon" onclick="downloadSinglePlaylist('${id}')" title="Download this playlist only">
                    ⬇️
                </button>
                <button type="button" class="btn-icon btn-danger-icon" onclick="removePlaylist('${id}')" title="Remove">
                    ✕
                </button>
            </div>
        </div>
    `).join('');

    loadPlaylistNames();
}

async function loadPlaylistNames() {
    for (const id of playlists) {
        try {
            const response = await fetch('/api/playlist-name?id=' + encodeURIComponent(id));
            if (!response.ok) continue;
            const data = await response.json();
            if (!data.name || data.name === id) continue;
            const item = document.querySelector(`.playlist-item[data-playlist-id="${id}"]`);
            if (item) {
                const nameEl = item.querySelector('.playlist-name');
                if (nameEl) nameEl.textContent = data.name;
            }
        } catch {
            // ignore per-playlist failures
        }
    }
}

function updatePlaylistDisplay() {
    const displayEl = document.getElementById('infoPlaylistCount');
    const listEl = document.getElementById('infoPlaylistList');
    const displayCountEl = document.getElementById('displayPlaylistCount');

    if (playlists.length === 0) {
        displayEl.textContent = '0';
        displayEl.classList.add('empty');
        listEl.textContent = '';
        displayCountEl.textContent = '0';
        displayCountEl.classList.add('empty');
    } else {
        displayEl.textContent = playlists.length;
        displayEl.classList.remove('empty');
        listEl.textContent = playlists.slice(0, 2).join(', ') + (playlists.length > 2 ? ', ...' : '');
        displayCountEl.textContent = playlists.length;
        displayCountEl.classList.remove('empty');
    }

    updateDownloadButton();
}

async function downloadSinglePlaylist(playlistId) {
    if (document.getElementById('downloadBtn').disabled) {
        showStatus('⚙️ Please configure settings first', 'warning');
        return;
    }

    const response = await fetch('/api/download', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ PlaylistId: playlistId })
    });
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
}

// ===== FILE NAMING =====
function updateFileNamePreview() {
    const pattern = document.getElementById('fileNamePattern').value;
    const preview = document.getElementById('fileNamePreview');

    const examples = {
        'track': 'Song Name.mp3',
        'artist-track': 'Artist Name - Song Name.mp3',
        'track-artist': 'Song Name - Artist Name.mp3',
        'artist-album-track': 'Artist Name - Album Name - Song Name.mp3'
    };

    preview.textContent = 'Example: ' + (examples[pattern] || examples['track']);
}

function getFileNamePatternLabel(pattern) {
    const labels = {
        'track': 'Track Name',
        'artist-track': 'Artist - Track',
        'track-artist': 'Track - Artist',
        'artist-album-track': 'Artist - Album - Track'
    };
    return labels[pattern] || labels['track'];
}

// ===== DEFAULT FOLDER HELPERS =====
function setDefaultMusicFolder() {
    const isWindows = navigator.platform.toLowerCase().includes('win');
    const path = isWindows
        ? 'C:\\Users\\' + (localStorage.getItem('username') || 'YourUsername') + '\\Music\\SpotifyDownloads'
        : '/home/' + (localStorage.getItem('username') || 'user') + '/Music/SpotifyDownloads';
    document.getElementById('downloadFolder').value = path;
}

function setDefaultDownloadsFolder() {
    const isWindows = navigator.platform.toLowerCase().includes('win');
    const path = isWindows
        ? 'C:\\Users\\' + (localStorage.getItem('username') || 'YourUsername') + '\\Downloads\\SpotifyDownloads'
        : '/home/' + (localStorage.getItem('username') || 'user') + '/Downloads/SpotifyDownloads';
    document.getElementById('downloadFolder').value = path;
}

// ===== TOOLS CHECK =====
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

// ===== STATS =====
async function loadStats() {
    try {
        const response = await fetch('/api/stats');
        const stats = await response.json();
        document.getElementById('infoTotalDownloaded').textContent = stats.total_downloaded || 0;
    } catch (e) {
        console.error('Failed to load stats:', e);
    }
}

// ===== FORM SUBMISSION =====
document.getElementById('configForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const config = {
        client_id: document.getElementById('clientId').value,
        client_secret: document.getElementById('clientSecret').value,
        playlist_ids: playlists,
        download_folder: document.getElementById('downloadFolder').value,
        file_name_pattern: document.getElementById('fileNamePattern').value,
        sponsorblock: document.getElementById('sponsorblock').checked,
        normalize_volume: document.getElementById('normalizeVolume').checked,
        auto_sync: document.getElementById('autoSync').checked
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

// ===== SPOTIFY DOWNLOAD =====
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
        showStatus('✗ ' + (data.error || 'Unknown error'), 'error');
    }
});

// ===== CLEAR HISTORY =====
document.getElementById('clearHistoryBtn').addEventListener('click', async () => {
    if (!confirm('⚠️ This will clear all download history and re-download ALL tracks from the playlist.\n\nAre you sure you want to continue?')) {
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

// ===== STATUS MESSAGES =====
function showStatus(message, type) {
    const statusEl = document.getElementById('statusMessage');
    statusEl.className = 'status ' + type;
    statusEl.textContent = message;
    setTimeout(() => statusEl.textContent = '', 5000);
}

// ===== PROGRESS POLLING =====
let pollingInterval;

function startProgressPolling() {
    pollingInterval = setInterval(async () => {
        const response = await fetch('/api/download/progress');
        const state = await response.json();

        updateProgress(state);

        if (!state.in_progress && state.total > 0) {
            clearInterval(pollingInterval);
            if (state.is_cancelled) {
                showStatus('⏹ Download cancelled', 'warning');
            }
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

    const cancelBtn = document.getElementById('cancelBtn');
    if (cancelBtn) {
        cancelBtn.style.display = state.in_progress ? 'block' : 'none';
    }

    const resultsList = document.getElementById('resultsList');
    resultsList.innerHTML = state.results.map(r => `
        <div class="result-item ${r.success ? 'success' : 'error'}">
            <div class="result-track">${r.track}</div>
            <div class="result-artist">${r.artist}</div>
            ${r.error ? `<div class="result-error">${r.error}</div>` : ''}
        </div>
    `).reverse().join('');
}

// ===== OPEN FOLDER =====
async function openFolder() {
    try {
        const response = await fetch('/api/open-folder', { method: 'POST' });
        if (!response.ok) {
            const data = await response.json();
            showStatus('✗ ' + (data.error || 'Could not open folder'), 'error');
        }
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    }
}

// ===== CANCEL DOWNLOAD =====
async function cancelDownload() {
    try {
        await fetch('/api/download/cancel', { method: 'POST' });
        showStatus('⏹ Cancelling download...', 'warning');
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    }
}

// ===== YOUTUBE DOWNLOAD =====
async function youtubeDownload() {
    const url = document.getElementById('youtubeUrl').value.trim();

    if (!url) {
        showYoutubeStatus('⚠️ Please enter a YouTube URL', 'warning');
        return;
    }

    const btn = document.getElementById('youtubeDownloadBtn');
    btn.disabled = true;
    btn.innerHTML = '<span>⏳ Downloading...</span>';

    document.getElementById('youtubeProgressSection').classList.add('active');
    document.getElementById('youtubeCurrentTitle').textContent =
        document.getElementById('youtubeTitle').value.trim() || url;
    document.getElementById('youtubeResultItem').innerHTML = '';

    const request = {
        url,
        title: document.getElementById('youtubeTitle').value.trim(),
        artist: document.getElementById('youtubeArtist').value.trim(),
        album: document.getElementById('youtubeAlbum').value.trim(),
        year: document.getElementById('youtubeYear').value.trim(),
        cover_art_url: document.getElementById('youtubeCoverArt').value.trim(),
        download_folder: document.getElementById('youtubeFolder').value.trim() || null
    };

    const response = await fetch('/api/youtube/download', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
    });

    const data = await response.json();

    if (!response.ok) {
        showYoutubeStatus('✗ ' + data.error, 'error');
        btn.disabled = false;
        btn.innerHTML = '<span>▶️ Download from YouTube</span>';
        document.getElementById('youtubeProgressSection').classList.remove('active');
        return;
    }

    startYoutubeProgressPolling();
}

let youtubePollingInterval;

function startYoutubeProgressPolling() {
    youtubePollingInterval = setInterval(async () => {
        const response = await fetch('/api/youtube/progress');
        const state = await response.json();

        if (!state.in_progress && state.success !== null) {
            clearInterval(youtubePollingInterval);

            const btn = document.getElementById('youtubeDownloadBtn');
            btn.disabled = false;
            btn.innerHTML = '<span>▶️ Download from YouTube</span>';

            const resultEl = document.getElementById('youtubeResultItem');

            if (state.success === true) {
                resultEl.innerHTML = '<div class="result-item success"><div class="result-track">✓ Download complete</div></div>';
                showYoutubeStatus('✓ Downloaded successfully!', 'success');
                loadStats();
            } else {
                resultEl.innerHTML = `<div class="result-item error"><div class="result-track">✗ Download failed</div><div class="result-error">${state.error || 'Unknown error'}</div></div>`;
                showYoutubeStatus('✗ ' + (state.error || 'Download failed'), 'error');
            }
        }
    }, 500);
}

function showYoutubeStatus(message, type) {
    const el = document.getElementById('youtubeStatus');
    el.className = 'status ' + type;
    el.textContent = message;
    setTimeout(() => el.textContent = '', 6000);
}

// ===== YOUTUBE FETCH INFO =====
async function fetchYoutubeInfo() {
    const url = document.getElementById('youtubeUrl').value.trim();

    if (!url) {
        showYoutubeStatus('⚠️ Please enter a YouTube URL first', 'warning');
        return;
    }

    const btn = document.getElementById('fetchInfoBtn');
    btn.disabled = true;
    btn.textContent = '⏳ Fetching...';

    try {
        const response = await fetch('/api/youtube/info?url=' + encodeURIComponent(url));
        const data = await response.json();

        if (response.ok) {
            if (data.title) document.getElementById('youtubeTitle').value = data.title;
            if (data.uploader) document.getElementById('youtubeArtist').value = data.uploader;
            showYoutubeStatus('✓ Info fetched!', 'success');
        } else {
            showYoutubeStatus('✗ ' + (data.error || 'Could not fetch info'), 'error');
        }
    } catch (e) {
        showYoutubeStatus('✗ Error: ' + e.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = '🔍 Fetch Info';
    }
}

// ===== INITIALIZE =====
loadConfig();
checkTools();
loadStats();