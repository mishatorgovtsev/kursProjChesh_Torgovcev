// ═══════════════════════════════════════════════════
//  Chess Platform — admin.js
//  Логика административной панели
// ═══════════════════════════════════════════════════

var ADMIN_TOKEN_KEY = 'chess_admin_token';
var adminToken      = null;
var currentSection  = 'dashboard';

// ── Инициализация ─────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    adminToken = localStorage.getItem(ADMIN_TOKEN_KEY);

    if (adminToken) {
        showPanel();
    } else {
        document.getElementById('login-screen').style.display = 'flex';
        document.getElementById('admin-panel').style.display  = 'none';
    }

    // Обновляем время в хедере каждую секунду
    setInterval(function () {
        var now = new Date();
        var t   = document.getElementById('admin-time');
        if (t) t.textContent = now.toLocaleTimeString('ru-RU');
    }, 1000);

    // Закрыть модал по клику на оверлей
    document.getElementById('modal-overlay').addEventListener('click', function (e) {
        if (e.target === document.getElementById('modal-overlay')) closeModal();
    });
});

// ── Логин ─────────────────────────────────────────
async function doAdminLogin() {
    var login = document.getElementById('admin-login').value.trim();
    var pass  = document.getElementById('admin-pass').value;
    var errEl = document.getElementById('login-err');
    errEl.textContent = '';

    if (!login || !pass) { errEl.textContent = 'Введите логин и пароль'; return; }

    try {
        var res  = await fetch('/api/Admin/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username: login, password: pass })
        });
        var data = await res.json();

        if (res.ok) {
            adminToken = data.token;
            localStorage.setItem(ADMIN_TOKEN_KEY, adminToken);
            showPanel();
        } else {
            errEl.textContent = data.error || 'Неверные данные';
        }
    } catch (e) {
        errEl.textContent = 'Ошибка сети';
    }
}

function adminLogout() {
    localStorage.removeItem(ADMIN_TOKEN_KEY);
    adminToken = null;
    document.getElementById('login-screen').style.display = 'flex';
    document.getElementById('admin-panel').style.display  = 'none';
}

function showPanel() {
    document.getElementById('login-screen').style.display = 'none';
    document.getElementById('admin-panel').style.display  = 'flex';
    showSection('dashboard');
}

// ── Навигация ─────────────────────────────────────
function showSection(name) {
    currentSection = name;

    document.querySelectorAll('.section').forEach(function (s) { s.classList.remove('active'); });
    document.querySelectorAll('.nav-item').forEach(function (n) { n.classList.remove('active'); });

    document.getElementById('section-' + name).classList.add('active');
    document.getElementById('nav-' + name).classList.add('active');

    var titles = { dashboard: 'Дашборд', users: 'Пользователи', games: 'Игры', tournaments: 'Турниры' };
    document.getElementById('section-title').textContent = titles[name] || name;

    if (name === 'dashboard')   loadStats();
    if (name === 'users')       loadUsers();
    if (name === 'games')       loadGames();
    if (name === 'tournaments') loadTournaments();
}

// ── API helper ────────────────────────────────────
async function api(url, options) {
    options = options || {};
    options.headers = Object.assign({ 'X-Admin-Token': adminToken }, options.headers || {});
    var res = await fetch(url, options);
    if (res.status === 401) { adminLogout(); return null; }
    return res;
}

// ── Дашборд ───────────────────────────────────────
async function loadStats() {
    try {
        var res  = await api('/api/Admin/stats');
        if (!res) return;
        var data = await res.json();

        document.getElementById('stat-users').textContent           = data.totalUsers;
        document.getElementById('stat-users-active').textContent    = data.activeUsers + ' активных';
        document.getElementById('stat-games').textContent           = data.totalGames;
        document.getElementById('stat-games-active').textContent    = data.activeGames + ' активных';
        document.getElementById('stat-completed').textContent       = data.completedGames;
        document.getElementById('stat-tournaments').textContent     = data.totalTournaments;
        document.getElementById('stat-tournaments-active').textContent = data.activeTournaments + ' активных';
    } catch (e) {
        console.error('Ошибка загрузки статистики:', e);
    }
}

// ── Пользователи ──────────────────────────────────
async function loadUsers() {
    var tbody  = document.getElementById('users-tbody');
    var search = document.getElementById('user-search').value.trim();
    var sort   = document.getElementById('user-sort').value;
    var order  = document.getElementById('user-order').value;

    tbody.innerHTML = '<tr class="loading-row"><td colspan="9">Загрузка…</td></tr>';

    try {
        var url = '/api/Admin/users?sort=' + sort + '&order=' + order;
        if (search) url += '&search=' + encodeURIComponent(search);

        var res   = await api(url);
        if (!res) return;
        var users = await res.json();

        if (!users.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Пользователи не найдены</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(function (u) {
            var statusBadge = u.isActive
                ? '<span class="badge badge-active">Активен</span>'
                : '<span class="badge badge-blocked">Заблокирован</span>';
            var created = u.createdAt ? new Date(u.createdAt).toLocaleDateString('ru-RU') : '—';

            return '<tr>' +
                '<td class="td-muted">#' + u.id + '</td>' +
                '<td class="td-accent">' + escHtml(u.username) + '</td>' +
                '<td class="td-muted">' + escHtml(u.email) + '</td>' +
                '<td><strong>' + u.rating + '</strong></td>' +
                '<td>' + (u.gamesPlayed || 0) + '</td>' +
                '<td><span class="td-green">' + (u.wins || 0) + '</span> / ' +
                '<span class="td-red">' + (u.losses || 0) + '</span> / ' +
                (u.draws || 0) + '</td>' +
                '<td>' + statusBadge + '</td>' +
                '<td class="td-muted">' + created + '</td>' +
                '<td><div class="act-btns">' +
                '<button class="act-btn" onclick="openEditRating(' + u.id + ',\'' + escHtml(u.username) + '\',' + u.rating + ')">Рейтинг</button>' +
                '<button class="act-btn" onclick="resetRating(' + u.id + ',\'' + escHtml(u.username) + '\')">Сброс</button>' +
                '<button class="act-btn" onclick="toggleActive(' + u.id + ',' + u.isActive + ',\'' + escHtml(u.username) + '\')">' +
                (u.isActive ? 'Блок' : 'Разблок') +
                '</button>' +
                '<button class="act-btn danger" onclick="deleteUser(' + u.id + ',\'' + escHtml(u.username) + '\')">Удалить</button>' +
                '</div></td>' +
                '</tr>';
        }).join('');

    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Ошибка загрузки</td></tr>';
    }
}

async function submitAddUser() {
    var username = document.getElementById('new-username').value.trim();
    var email    = document.getElementById('new-email').value.trim();
    var password = document.getElementById('new-password').value;
    var rating   = parseInt(document.getElementById('new-rating').value) || 1200;
    var errEl    = document.getElementById('add-user-err');
    errEl.textContent = '';

    if (!username || !email || !password) { errEl.textContent = 'Заполните все поля'; return; }

    try {
        var res  = await api('/api/Admin/users', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password, rating })
        });
        var data = await res.json();
        if (res.ok) {
            closeModal();
            toast('Пользователь создан!', 'success');
            loadUsers();
        } else {
            errEl.textContent = data.error || 'Ошибка';
        }
    } catch (e) { errEl.textContent = 'Ошибка сети'; }
}

function openEditRating(id, username, currentRating) {
    document.getElementById('er-userid').value   = id;
    document.getElementById('er-username').value = username;
    document.getElementById('er-rating').value   = currentRating;
    openModal('edit-rating');
}

async function submitEditRating() {
    var id     = parseInt(document.getElementById('er-userid').value);
    var rating = parseInt(document.getElementById('er-rating').value);
    if (!rating || rating < 0) { toast('Введите корректный рейтинг', 'error'); return; }

    try {
        var res = await api('/api/Admin/users/' + id + '/rating', {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ rating })
        });
        if (res.ok) { closeModal(); toast('Рейтинг обновлён', 'success'); loadUsers(); }
        else { var d = await res.json(); toast(d.error || 'Ошибка', 'error'); }
    } catch (e) { toast('Ошибка сети', 'error'); }
}

async function resetRating(id, username) {
    if (!confirm('Сбросить рейтинг ' + username + ' до 1200?')) return;
    try {
        var res = await api('/api/Admin/users/' + id + '/reset-rating', { method: 'POST' });
        if (res.ok) { toast('Рейтинг сброшен', 'success'); loadUsers(); }
        else toast('Ошибка', 'error');
    } catch (e) { toast('Ошибка', 'error'); }
}

async function toggleActive(id, isActive, username) {
    var action = isActive ? 'заблокировать' : 'разблокировать';
    if (!confirm(action.charAt(0).toUpperCase() + action.slice(1) + ' ' + username + '?')) return;
    try {
        var res = await api('/api/Admin/users/' + id + '/active', {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isActive: !isActive })
        });
        if (res.ok) { toast('Готово', 'success'); loadUsers(); }
        else toast('Ошибка', 'error');
    } catch (e) { toast('Ошибка', 'error'); }
}

async function deleteUser(id, username) {
    if (!confirm('Удалить пользователя ' + username + '? Это действие необратимо!')) return;
    try {
        var res = await api('/api/Admin/users/' + id, { method: 'DELETE' });
        if (res.ok) { toast('Пользователь удалён', 'success'); loadUsers(); }
        else { var d = await res.json(); toast(d.error || 'Ошибка', 'error'); }
    } catch (e) { toast('Ошибка', 'error'); }
}

// ── Игры ──────────────────────────────────────────
async function loadGames() {
    var tbody  = document.getElementById('games-tbody');
    var userId = document.getElementById('game-user-filter').value.trim();
    var status = document.getElementById('game-status-filter').value;
    var sort   = document.getElementById('game-sort').value;
    var order  = document.getElementById('game-order').value;

    tbody.innerHTML = '<tr class="loading-row"><td colspan="9">Загрузка…</td></tr>';

    try {
        var url = '/api/Admin/games?sort=' + sort + '&order=' + order;
        if (userId) url += '&userId=' + userId;
        if (status) url += '&status=' + status;

        var res   = await api(url);
        if (!res) return;
        var games = await res.json();

        if (!games.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Игры не найдены</td></tr>';
            return;
        }

        var statusLabels = { Pending: 'Ожидание', InProgress: 'Идёт', Completed: 'Завершена' };
        var statusClass  = { Pending: 'badge-pending', InProgress: 'badge-active', Completed: 'badge-completed' };

        tbody.innerHTML = games.map(function (g) {
            var winner  = g.winner ? escHtml(g.winner.username) : '—';
            var created = g.createdAt ? new Date(g.createdAt).toLocaleDateString('ru-RU') : '—';
            var ended   = g.endedAt   ? new Date(g.endedAt).toLocaleDateString('ru-RU')   : '—';

            return '<tr>' +
                '<td class="td-muted">#' + g.id + '</td>' +
                '<td>' + escHtml(g.whitePlayer.username) + '</td>' +
                '<td>' + escHtml(g.blackPlayer.username) + '</td>' +
                '<td class="td-accent">' + winner + '</td>' +
                '<td><span class="badge ' + (statusClass[g.status] || 'badge-pending') + '">' +
                (statusLabels[g.status] || g.status) + '</span></td>' +
                '<td class="td-muted">' + g.timeControlMinutes + ' мин</td>' +
                '<td class="td-muted">' + created + '</td>' +
                '<td class="td-muted">' + ended + '</td>' +
                '<td><div class="act-btns">' +
                (g.status !== 'Completed'
                    ? '<button class="act-btn success" onclick="openFinishGame(' + g.id + ')">Завершить</button>'
                    : '') +
                '<button class="act-btn danger" onclick="deleteGame(' + g.id + ')">Удалить</button>' +
                '</div></td>' +
                '</tr>';
        }).join('');

    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Ошибка загрузки</td></tr>';
    }
}

async function submitAddGame() {
    var white = parseInt(document.getElementById('ag-white').value);
    var black = parseInt(document.getElementById('ag-black').value);
    var time  = parseInt(document.getElementById('ag-time').value);
    var errEl = document.getElementById('add-game-err');
    errEl.textContent = '';

    if (!white || !black) { errEl.textContent = 'Введите ID обоих игроков'; return; }
    if (white === black)   { errEl.textContent = 'Игроки должны быть разными'; return; }

    try {
        var res  = await api('/api/Admin/games', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ whitePlayerId: white, blackPlayerId: black, timeControlMinutes: time })
        });
        var data = await res.json();
        if (res.ok) { closeModal(); toast('Игра создана! ID: ' + data.gameId, 'success'); loadGames(); }
        else errEl.textContent = data.error || 'Ошибка';
    } catch (e) { errEl.textContent = 'Ошибка сети'; }
}

function openFinishGame(gameId) {
    document.getElementById('fg-gameid').value = gameId;
    openModal('finish-game');
}

async function submitFinishGame() {
    var id     = parseInt(document.getElementById('fg-gameid').value);
    var result = document.getElementById('fg-result').value;
    try {
        var res = await api('/api/Admin/games/' + id + '/finish', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result })
        });
        if (res.ok) { closeModal(); toast('Игра завершена', 'success'); loadGames(); }
        else { var d = await res.json(); toast(d.error || 'Ошибка', 'error'); }
    } catch (e) { toast('Ошибка', 'error'); }
}

async function deleteGame(id) {
    if (!confirm('Удалить игру #' + id + '?')) return;
    try {
        var res = await api('/api/Admin/games/' + id, { method: 'DELETE' });
        if (res.ok) { toast('Игра удалена', 'success'); loadGames(); }
        else toast('Ошибка', 'error');
    } catch (e) { toast('Ошибка', 'error'); }
}

// ── Турниры ───────────────────────────────────────
async function loadTournaments() {
    var tbody  = document.getElementById('tournaments-tbody');
    var status = document.getElementById('t-status-filter').value;
    var sort   = document.getElementById('t-sort').value;
    var order  = document.getElementById('t-order').value;

    tbody.innerHTML = '<tr class="loading-row"><td colspan="9">Загрузка…</td></tr>';

    try {
        var url = '/api/Admin/tournaments?sort=' + sort + '&order=' + order;
        if (status) url += '&status=' + status;

        var res  = await api(url);
        if (!res) return;
        var list = await res.json();

        if (!list.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Турниры не найдены</td></tr>';
            return;
        }

        var sLabels = { Registration: 'Регистрация', InProgress: 'Идёт', Completed: 'Завершён' };
        var sClass  = { Registration: 'badge-reg', InProgress: 'badge-active', Completed: 'badge-completed' };

        tbody.innerHTML = list.map(function (t) {
            var winner  = t.winner  ? escHtml(t.winner.username)  : '—';
            var creator = t.creator ? escHtml(t.creator.username) : '—';
            var starts  = t.startsAt ? new Date(t.startsAt).toLocaleDateString('ru-RU') : '—';

            return '<tr>' +
                '<td class="td-muted">#' + t.id + '</td>' +
                '<td class="td-accent">' + escHtml(t.title) + '</td>' +
                '<td class="td-muted">' + creator + '</td>' +
                '<td>' + (t.currentParticipants || 0) + ' / ' + t.maxParticipants + '</td>' +
                '<td class="td-muted">' + t.timeControlMinutes + ' мин</td>' +
                '<td><span class="badge ' + (sClass[t.status] || 'badge-pending') + '">' +
                (sLabels[t.status] || t.status) + '</span></td>' +
                '<td class="td-muted">' + starts + '</td>' +
                '<td class="td-accent">' + winner + '</td>' +
                '<td><div class="act-btns">' +
                '<button class="act-btn" onclick="openTournamentStatus(' + t.id + ',\'' + t.status + '\')">Статус</button>' +
                '<button class="act-btn danger" onclick="deleteTournament(' + t.id + ',\'' + escHtml(t.title) + '\')">Удалить</button>' +
                '</div></td>' +
                '</tr>';
        }).join('');

    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="empty-table">Ошибка загрузки</td></tr>';
    }
}

function openTournamentStatus(id, currentStatus) {
    document.getElementById('ts-id').value           = id;
    document.getElementById('ts-status').value       = currentStatus;
    openModal('t-status');
}

async function submitTournamentStatus() {
    var id     = parseInt(document.getElementById('ts-id').value);
    var status = document.getElementById('ts-status').value;
    try {
        var res = await api('/api/Admin/tournaments/' + id + '/status', {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ status })
        });
        if (res.ok) { closeModal(); toast('Статус обновлён', 'success'); loadTournaments(); }
        else { var d = await res.json(); toast(d.error || 'Ошибка', 'error'); }
    } catch (e) { toast('Ошибка', 'error'); }
}

async function deleteTournament(id, title) {
    if (!confirm('Удалить турнир "' + title + '"?')) return;
    try {
        var res = await api('/api/Admin/tournaments/' + id, { method: 'DELETE' });
        if (res.ok) { toast('Турнир удалён', 'success'); loadTournaments(); }
        else toast('Ошибка', 'error');
    } catch (e) { toast('Ошибка', 'error'); }
}

// ── Модалы ────────────────────────────────────────
function openModal(name) {
    document.querySelectorAll('.admin-modal').forEach(function (m) { m.style.display = 'none'; });
    var modal = document.getElementById('modal-' + name);
    if (modal) modal.style.display = 'block';
    document.getElementById('modal-overlay').classList.add('open');
}

function closeModal() {
    document.getElementById('modal-overlay').classList.remove('open');
    document.querySelectorAll('.admin-modal').forEach(function (m) { m.style.display = 'none'; });
}