// Global state
let currentConfig = {};
let playlists = [];

// ===== TAB SWITCHING =====
function switchTab(tabName) {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));

    event.target.classList.add('active');
    document.getElementById(tabName + '-tab').classList.add('active');
}

// ===== CONFIGURATION MANAGEMENT =====
async function loadConfig() {
    const response = await fetch('/api/config');
    currentConfig = await response.json();

    // Update form inputs
    document.getElementById('clientId').value = currentConfig.client_id || '';
    document.getElementById('clientSecret').value = currentConfig.client_secret || '';
    document.getElementById('downloadFolder').value = currentConfig.download_folder || '';
    document.getElementById('fileNamePattern').value = currentConfig.file_name_pattern || 'track';

    // Load playlists
    playlists = currentConfig.playlist_ids || [];
    renderPlaylistList();

    // Update display values
    updateDisplayValue('displayClientId', currentConfig.client_id);
    updateDisplayValue('displayClientSecret', currentConfig.client_secret);
    updateDisplayValue('displayDownloadFolder', currentConfig.download_folder);
    updateDisplayValue('displayFileNamePattern', getFileNamePatternLabel(currentConfig.file_name_pattern));

    // Update dashboard info
    updateDisplayValue('infoDownloadFolder', currentConfig.download_folder);
    updatePlaylistDisplay();
    updateFileNamePreview();

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
        <div class="playlist-item">
            <div class="playlist-info">
                <div class="playlist-number">${index + 1}</div>
                <div class="playlist-id">${id}</div>
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
        body: JSON.stringify({ playlist_id: playlistId })
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
        file_name_pattern: document.getElementById('fileNamePattern').value
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

// ===== DOWNLOAD =====
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
        <div class="result-item ${r.success ? 'success' : 'error'}">
            <div class="result-track">${r.track}</div>
            <div class="result-artist">${r.artist}</div>
            ${r.error ? `<div class="result-error">${r.error}</div>` : ''}
        </div>
    `).reverse().join('');
}

// ===== INITIALIZE =====
loadConfig();
checkTools();
loadStats();