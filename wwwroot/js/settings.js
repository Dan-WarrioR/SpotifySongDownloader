let currentConfig = {};
let playlists = [];
let ytmPlaylists = [];

// ===== INIT =====
async function init() {
    await loadConfig();
}

// ===== CONFIG LOAD =====
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

    ytmPlaylists = currentConfig.ytm_playlist_ids || [];
    renderYtmPlaylistList();
    document.getElementById('ytmCookiesBrowser').value = currentConfig.ytm_cookies_browser || '';

    updateFileNamePreview();
}

// ===== SPOTIFY CONFIG =====
async function saveSpotifyConfig() {
    const config = {
        ...currentConfig,
        client_id: document.getElementById('clientId').value,
        client_secret: document.getElementById('clientSecret').value,
        playlist_ids: playlists,
        download_folder: document.getElementById('downloadFolder').value,
        file_name_pattern: document.getElementById('fileNamePattern').value,
        sponsorblock: document.getElementById('sponsorblock').checked,
        normalize_volume: document.getElementById('normalizeVolume').checked,
        auto_sync: document.getElementById('autoSync').checked
    };

    try {
        await fetch('/api/config', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });
        showSpotifyConfigStatus('✓ Spotify config saved!', 'success');
        await loadConfig();
    } catch (e) {
        showSpotifyConfigStatus('✗ Save failed: ' + e.message, 'error');
    }
}

function showSpotifyConfigStatus(message, type) {
    const el = document.getElementById('spotifyConfigStatus');
    el.className = 'status ' + type;
    el.textContent = message;
    setTimeout(() => { el.textContent = ''; el.className = ''; }, 4000);
}

// ===== SPOTIFY PLAYLIST MANAGEMENT =====
function addPlaylist() {
    const input = document.getElementById('newPlaylistId');
    const playlistId = input.value.trim();

    if (!playlistId) {
        showSpotifyConfigStatus('⚠️ Please enter a playlist ID', 'warning');
        return;
    }

    if (playlists.includes(playlistId)) {
        showSpotifyConfigStatus('⚠️ Playlist already added', 'warning');
        return;
    }

    playlists.push(playlistId);
    input.value = '';
    renderPlaylistList();
    showSpotifyConfigStatus('✓ Playlist added — remember to save', 'success');
}

function removePlaylist(playlistId) {
    playlists = playlists.filter(id => id !== playlistId);
    renderPlaylistList();
}

function renderPlaylistList() {
    const container = document.getElementById('playlistList');
    if (!container) return;

    if (playlists.length === 0) {
        container.innerHTML = '<div style="text-align: center; color: var(--text-secondary); padding: 20px;">No playlists added yet</div>';
        return;
    }

    container.innerHTML = playlists.map((id, index) => `
        <div class="playlist-item">
            <div class="playlist-info">
                <div class="playlist-number">${index + 1}</div>
                <div><div class="playlist-id">${id}</div></div>
            </div>
            <div class="playlist-actions">
                <button type="button" class="btn-icon btn-danger-icon" onclick="removePlaylist('${id}')" title="Remove">✕</button>
            </div>
        </div>
    `).join('');
}

// ===== YTM CONFIG =====
async function saveYtmConfig() {
    const config = {
        ...currentConfig,
        ytm_playlist_ids: ytmPlaylists,
        ytm_cookies_browser: document.getElementById('ytmCookiesBrowser').value
    };

    try {
        await fetch('/api/config', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });
        showYtmConfigStatus('✓ YouTube Music config saved!', 'success');
        await loadConfig();
    } catch (e) {
        showYtmConfigStatus('✗ Save failed: ' + e.message, 'error');
    }
}

function showYtmConfigStatus(message, type) {
    const el = document.getElementById('ytmConfigStatus');
    el.className = 'status ' + type;
    el.textContent = message;
    setTimeout(() => { el.textContent = ''; el.className = ''; }, 4000);
}

// ===== YTM PLAYLIST MANAGEMENT =====
function addYtmPlaylist() {
    const input = document.getElementById('newYtmPlaylistId');
    const playlistId = input.value.trim();

    if (!playlistId) {
        showYtmConfigStatus('⚠️ Please enter a playlist ID', 'warning');
        return;
    }

    if (ytmPlaylists.includes(playlistId)) {
        showYtmConfigStatus('⚠️ Playlist already added', 'warning');
        return;
    }

    ytmPlaylists.push(playlistId);
    input.value = '';
    renderYtmPlaylistList();
    showYtmConfigStatus('✓ Playlist added — remember to save', 'success');
}

function removeYtmPlaylist(playlistId) {
    ytmPlaylists = ytmPlaylists.filter(id => id !== playlistId);
    renderYtmPlaylistList();
}

function renderYtmPlaylistList() {
    const container = document.getElementById('ytmPlaylistList');
    if (!container) return;

    if (ytmPlaylists.length === 0) {
        container.innerHTML = '<div style="text-align: center; color: var(--text-secondary); padding: 20px;">No playlists added yet</div>';
        return;
    }

    container.innerHTML = ytmPlaylists.map((id, index) => `
        <div class="playlist-item">
            <div class="playlist-info">
                <div class="playlist-number">${index + 1}</div>
                <div><div class="playlist-id">${id}</div></div>
            </div>
            <div class="playlist-actions">
                <button type="button" class="btn-icon btn-danger-icon" onclick="removeYtmPlaylist('${id}')" title="Remove">✕</button>
            </div>
        </div>
    `).join('');
}

// ===== FILE NAMING =====
function updateFileNamePreview() {
    const pattern = document.getElementById('fileNamePattern').value;
    const examples = {
        'track': 'Song Name.mp3',
        'artist-track': 'Artist Name - Song Name.mp3',
        'track-artist': 'Song Name - Artist Name.mp3',
        'artist-album-track': 'Artist Name - Album Name - Song Name.mp3'
    };
    document.getElementById('fileNamePreview').textContent = 'Example: ' + (examples[pattern] || examples['track']);
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

init();
