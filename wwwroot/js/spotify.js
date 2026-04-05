let currentConfig = {};
let playlists = [];

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

    playlists = currentConfig.playlist_ids || [];

    updateDisplayValue('infoDownloadFolder', currentConfig.download_folder);
    updatePlaylistDisplay();
    updateDownloadButton();
}

function updateDisplayValue(elementId, value) {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (value && value.trim() !== '') {
        el.textContent = value;
        el.classList.remove('empty');
    } else {
        el.textContent = 'Not configured';
        el.classList.add('empty');
    }
}

function updatePlaylistDisplay() {
    const countEl = document.getElementById('infoPlaylistCount');
    const listEl = document.getElementById('infoPlaylistList');
    if (!countEl || !listEl) return;

    if (playlists.length === 0) {
        countEl.textContent = '0';
        countEl.classList.add('empty');
        listEl.textContent = '';
    } else {
        countEl.textContent = playlists.length;
        countEl.classList.remove('empty');
        listEl.textContent = playlists.slice(0, 2).join(', ') + (playlists.length > 2 ? ', ...' : '');
    }

    updateDownloadButton();
}

function updateDownloadButton() {
    const btn = document.getElementById('downloadBtn');
    if (!btn) return;
    const isConfigured = currentConfig.client_id && currentConfig.client_secret &&
        playlists.length > 0 && currentConfig.download_folder;
    btn.disabled = !isConfigured;
    btn.innerHTML = isConfigured
        ? `<span>⬇️ Download All Playlists (${playlists.length})</span>`
        : '<span>⚙️ Configure Settings First</span>';
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
            showStatus('✓ yt-dlp updated to latest version!', 'success');
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
        document.getElementById('infoTotalDownloaded').textContent = stats.total_downloaded || 0;
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
    const el = document.getElementById('statusMessage');
    el.className = 'status ' + type;
    el.textContent = message;
    setTimeout(() => { el.textContent = ''; el.className = ''; }, 5000);
}

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
        showStatus('✗ ' + (data.error || 'Unknown error'), 'error');
    }
});

let pollingInterval;

function startProgressPolling() {
    pollingInterval = setInterval(async () => {
        const response = await fetch('/api/download/progress');
        const state = await response.json();
        updateProgress(state);
        if (!state.in_progress && state.total > 0) {
            clearInterval(pollingInterval);
            if (state.is_cancelled) showStatus('⏹ Download cancelled', 'warning');
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
    if (cancelBtn) cancelBtn.style.display = state.in_progress ? 'block' : 'none';

    document.getElementById('resultsList').innerHTML = state.results.map(r => `
        <div class="result-item ${r.success ? 'success' : 'error'}">
            <div class="result-track">${r.track}</div>
            <div class="result-artist">${r.artist}</div>
            ${r.error ? `<div class="result-error">${r.error}</div>` : ''}
        </div>
    `).reverse().join('');
}

async function cancelDownload() {
    try {
        await fetch('/api/download/cancel', { method: 'POST' });
        showStatus('⏹ Cancelling download...', 'warning');
    } catch (e) {
        showStatus('✗ Error: ' + e.message, 'error');
    }
}

// ===== CLEAR HISTORY =====
document.getElementById('clearHistoryBtn').addEventListener('click', async () => {
    if (!confirm('⚠️ This will clear all download history and re-download ALL tracks.\n\nAre you sure?')) return;
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
