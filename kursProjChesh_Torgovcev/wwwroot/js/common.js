// ═══════════════════════════════════════════════════
//  Chess Platform — common.js
//  Общие утилиты: currentUser, toast, навигация
// ═══════════════════════════════════════════════════

// ── Текущий пользователь ─────────────────────────
var currentUser = null;

function loadCurrentUser() {
    var saved = localStorage.getItem('chess_user');
    if (!saved) return null;
    try {
        currentUser = JSON.parse(saved);
        return currentUser;
    } catch(e) {
        localStorage.removeItem('chess_user');
        return null;
    }
}

function saveCurrentUser(data) {
    currentUser = data;
    localStorage.setItem('chess_user', JSON.stringify(data));
}

function logout() {
    localStorage.removeItem('chess_user');
    currentUser = null;
    window.location.href = '/';
}

// ── Toast ─────────────────────────────────────────
function toast(msg, type) {
    var el = document.getElementById('toast');
    if (!el) return;
    el.textContent = msg;
    el.className = 'toast ' + (type || 'success') + ' show';
    setTimeout(function() { el.className = 'toast'; }, 3000);
}

// ── Навигация ─────────────────────────────────────
function goHome() {
    window.location.href = '/';
}

function goToProfile(userId) {
    var id = userId || (currentUser && currentUser.userId);
    if (!id) return;
    window.location.href = '/profile.html?id=' + id;
}

function goToGame(gameId, whiteId, blackId, whiteName, blackName, timeMin, trainingMode) {
    // Сохраняем параметры игры в sessionStorage для game.html
    sessionStorage.setItem('chess_game', JSON.stringify({
        gameId:       gameId,
        whiteId:      whiteId,
        blackId:      blackId,
        whiteName:    whiteName,
        blackName:    blackName,
        timeMin:      timeMin || 10,
        trainingMode: trainingMode || false
    }));
    // gameId в URL — для восстановления после обновления страницы
    window.location.href = '/game.html?gameId=' + gameId;
}

// ── Инициализация header ──────────────────────────
function initHeader() {
    var user = loadCurrentUser();

    var headerUserEl = document.getElementById('header-user');
    if (headerUserEl && user) {
        headerUserEl.innerHTML = '<strong>' + escHtml(user.username) + '</strong> (' + user.rating + ')';
        headerUserEl.onclick = function() { goToProfile(user.userId); };
    }

    var logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.onclick = logout;
    }
}

// ── Утилиты ───────────────────────────────────────
function escHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g,'&amp;')
        .replace(/</g,'&lt;')
        .replace(/>/g,'&gt;');
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    var d = new Date(dateStr);
    return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatTime(sec) {
    var m = Math.floor(sec / 60);
    var s = sec % 60;
    return m.toString().padStart(2, '0') + ':' + s.toString().padStart(2, '0');
}