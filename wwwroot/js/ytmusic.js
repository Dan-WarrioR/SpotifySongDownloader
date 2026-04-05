let currentConfig = {};
let ytmPlaylists = [];

// ===== INIT =====
async function init() {
    await loadConfig();
    checkTools();
    loadStats();
}

// ===== CONFIG =====
async function loadConfig() {
    const response = await fetch('/api/config');
    currentConfig = await response.json();

    ytmPlaylists = currentConfig.ytm_playlist_ids || [];
    updateSetupDisplay();
    updateDownloadButton();
}

function updateSetupDisplay() {
    const countEl = document.getElementById('ytmInfoPlaylistCount');
    const listEl = document.getElementById('ytmInfoPlaylistList');
    const authEl = document.getElementById('ytmInfoAuth');
    const folderEl = document.getElementById('ytmInfoDownloadFolder');

    if (countEl) {
        if (ytmPlaylists.length === 0) {
            countEl.textContent = '0';
            countEl.classList.add('empty');
            if (listEl) listEl.textContent = '';
        } else {
            countEl.textContent = ytmPlaylists.length;
            countEl.classList.remove('empty');
            if (listEl) listEl.textContent = ytmPlaylists.slice(0, 2).join(', ') + (ytmPlaylists.length > 2 ? ', ...' : '');
        }
    }

    if (authEl) {
        const browser = currentConfig.ytm_cookies_browser || '';
        authEl.textContent = browser
            ? browser.charAt(0).toUpperCase() + browser.slice(1) + ' cookies'
            : 'None (public)';
        authEl.classList.toggle('empty', !browser);
    }

    if (folderEl) {
        const folder = currentConfig.download_folder || '';
        if (folder) {
            folderEl.textContent = folder;
            folderEl.classList.remove('empty');
        } else {
            folderEl.textContent = 'Not configured';
            folderEl.classList.add('empty');
        }
    }
}

function updateDownloadButton() {
    const btn = document.getElementById('ytmDownloadBtn');
    if (!btn) return;
    const isConfigured = ytmPlaylists.length > 0 && currentConfig.download_folder;
    btn.disabled = !isConfigured;
    btn.innerHTML = isConfigured
        ? `<span>⬇️ Download All Playlists (${ytmPlaylists.length})</span>`
        : '<span>⚙️ Configure Playlists in Settings</span>';
}

// ===== TOOLS =====
async function checkTools() {
    const [ytdlp, ffmpeg] = await Promise.all([
        fetch('/api/check-ytdlp').then(r => r.json()),
        fetch('/api/check-ffmpeg').then(r => r.json())
    ]);
    document.getElementById('ytdlpStatus').className = 'tool-status ' + (ytdlp.installed ? 'installed' : 'missing');
    document.getElementById('ffmpegStatus').className = 'tool-status ' + (ffmpeg.installed ? 'installed' : 'missing');
}

async function updateYtDlp() {
    const btn = document.getElementById('updateYtdlpBtn');
    btn.disabled = true;
    btn.textContent = '⏳ Updating...';
    try {
        const response = await fetch('/api/update-ytdlp', { method: 'POST' });
        const data = await response.json();
        if (data.updated) {
            showStatus('✓ yt-dlp updated!', 'success');
        } else if (data.already_up_to_date) {
            showStatus('ℹ️ yt-dlp is already up to date', 'warning');
        } else {
            showStatus('⚠️ ' + data.message, 'warning');
        }
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = '⬆️ Update';
    }
}

// ===== STATS =====
async function loadStats() {
    try {
        const response = await fetch('/api/stats');
        const stats = await response.json();
        const el = document.getElementById('ytmInfoTotalDownloaded');
        if (el) el.textContent = stats.total_downloaded || 0;
    } catch (e) {
        console.error('Failed to load stats:', e);
    }
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

// ===== STATUS =====
function showStatus(message, type) {
    const el = document.getElementById('ytmStatusMessage');
    el.className = 'status ' + type;
    el.textContent = message;
    setTimeout(() => { el.textContent = ''; el.className = ''; }, 5000);
}

// ===== DOWNLOAD =====
document.getElementById('ytmDownloadBtn').addEventListener('click', async () => {
    if (document.getElementById('ytmDownloadBtn').disabled) return;

    const response = await fetch('/api/ytmusic/download', { method: 'POST' });
    const data = await response.json();

    if (response.ok) {
        if (data.new === 0) {
            showStatus('ℹ️ ' + data.message, 'warning');
        } else {
            showStatus('✓ ' + data.message, 'success');
            document.getElementById('ytmProgressSection').classList.add('active');
            startProgressPolling();
        }
    } else {
        showStatus('✗ ' + (data.error || 'Unknown error'), 'error');
    }
});

let pollingInterval;

function escapeHtml(str) {
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function startProgressPolling() {
    pollingInterval = setInterval(async () => {
        try {
            const response = await fetch('/api/ytmusic/progress');
            const state = await response.json();
            updateProgress(state);
            if (!state.in_progress && state.total > 0) {
                clearInterval(pollingInterval);
                if (state.is_cancelled) showStatus('⏹ Download cancelled', 'warning');
                await loadStats();
            }
        } catch (e) {
            clearInterval(pollingInterval);
            showStatus('✗ Lost connection to server', 'error');
        }
    }, 500);
}

function updateProgress(state) {
    document.getElementById('ytmCurrentTrack').textContent = state.current_track || '';
    document.getElementById('ytmProgressText').textContent = state.progress + '%';
    document.getElementById('ytmProgressBar').style.width = state.progress + '%';
    document.getElementById('ytmCompletedCount').textContent = state.completed;
    document.getElementById('ytmFailedCount').textContent = state.failed;
    document.getElementById('ytmTotalCount').textContent = state.total;

    const cancelBtn = document.getElementById('ytmCancelBtn');
    if (cancelBtn) cancelBtn.style.display = state.in_progress ? 'block' : 'none';

    document.getElementById('ytmResultsList').innerHTML = state.results.map(r => `
        <div class="result-item ${r.success ? 'success' : 'error'}">
            <div class="result-track">${escapeHtml(r.track)}</div>
            <div class="result-artist">${escapeHtml(r.artist)}</div>
            ${r.error ? `<div class="result-error">${escapeHtml(r.error)}</div>` : ''}
        </div>
    `).reverse().join('');
}

async function cancelYtmDownload() {
    try {
        await fetch('/api/ytmusic/download/cancel', { method: 'POST' });
        showStatus('⏹ Cancelling download...', 'warning');
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    }
}

// ===== CLEAR HISTORY =====
document.getElementById('ytmClearHistoryBtn').addEventListener('click', async () => {
    if (!confirm('⚠️ This will clear all download history (Spotify + YT Music) and allow re-downloading all tracks.\n\nAre you sure?')) return;
    try {
        const response = await fetch('/api/clear-history', { method: 'POST' });
        const data = await response.json();
        if (response.ok) {
            showStatus('✓ Download history cleared!', 'success');
            await loadStats();
        } else {
            showStatus('✗ ' + data.error, 'error');
        }
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    }
});

init();
