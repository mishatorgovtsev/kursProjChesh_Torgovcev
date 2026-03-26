// ═══════════════════════════════════════════════════
//  Chess Platform — lobby.js
//  Логика главной страницы (лобби)
// ═══════════════════════════════════════════════════

var isProcessing = false;

// ── Инициализация ─────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var user = loadCurrentUser();
    if (user) {
        showLobby();
    } else {
        showAuthModal();
    }
});

function showAuthModal() {
    document.getElementById('auth-modal').classList.add('open');
    document.getElementById('main-header').style.display = 'none';
    document.getElementById('lobby').style.display = 'none';
}

function showLobby() {
    document.getElementById('auth-modal').classList.remove('open');
    document.getElementById('main-header').style.display = 'flex';
    document.getElementById('lobby').style.display = 'block';
    initHeader();
    loadLobby();
}

// ── Auth tabs ─────────────────────────────────────
function switchAuthTab(tab) {
    document.querySelectorAll('.auth-tab').forEach(function (t, i) {
        t.classList.toggle('active', (i === 0) === (tab === 'login'));
    });
    document.getElementById('login-form').classList.toggle('active', tab === 'login');
    document.getElementById('register-form').classList.toggle('active', tab === 'register');
    document.getElementById('login-error').textContent = '';
    document.getElementById('reg-error').textContent = '';
}

// ── Регистрация ───────────────────────────────────
async function doRegister() {
    if (isProcessing) return;
    var btn    = document.getElementById('reg-btn');
    var errEl  = document.getElementById('reg-error');
    errEl.textContent = '';

    var username = document.getElementById('reg-username').value.trim();
    var email    = document.getElementById('reg-email').value.trim();
    var password = document.getElementById('reg-password').value;

    if (!username || !email || !password) {
        errEl.textContent = 'Заполните все поля';
        return;
    }

    btn.disabled = true;
    isProcessing = true;
    btn.textContent = 'Регистрация…';

    try {
        var res  = await fetch('/api/Users/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });
        var data = await res.json();

        if (res.ok) {
            toast('Регистрация успешна! Войдите в систему.', 'success');
            switchAuthTab('login');
            document.getElementById('login-username').value = username;
        } else {
            errEl.textContent = data.error || data || 'Ошибка регистрации';
        }
    } catch (e) {
        errEl.textContent = 'Ошибка сети';
    } finally {
        btn.disabled    = false;
        isProcessing    = false;
        btn.textContent = 'Зарегистрироваться';
    }
}

// ── Вход ──────────────────────────────────────────
async function doLogin() {
    if (isProcessing) return;
    var btn   = document.getElementById('login-btn');
    var errEl = document.getElementById('login-error');
    errEl.textContent = '';

    var username = document.getElementById('login-username').value.trim();
    var password = document.getElementById('login-password').value;

    if (!username || !password) {
        errEl.textContent = 'Введите имя пользователя и пароль';
        return;
    }

    btn.disabled = true;
    isProcessing = true;
    btn.textContent = 'Входим…';

    try {
        var res  = await fetch('/api/Users/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        var data = await res.json();

        if (res.ok) {
            saveCurrentUser(data);
            showLobby();
        } else {
            errEl.textContent = data.error || 'Неверные данные';
        }
    } catch (e) {
        errEl.textContent = 'Ошибка сети';
    } finally {
        btn.disabled    = false;
        isProcessing    = false;
        btn.textContent = 'Войти';
    }
}

// ── Загрузка лобби ────────────────────────────────
async function loadLobby() {
    await Promise.all([
        loadOpponents(),
        loadLeaderboard(),
        loadActiveGames(),
        loadTournaments()
    ]);
}

// Список игроков для выбора противника
async function loadOpponents() {
    try {
        var res   = await fetch('/api/Users/leaderboard?top=50');
        var users = await res.json();
        var sel   = document.getElementById('opponent-select');
        sel.innerHTML = '';

        var others = users.filter(function (u) { return u.id !== currentUser.userId; });

        if (others.length === 0) {
            sel.innerHTML = '<option value="">Нет других игроков</option>';
            return;
        }
        others.forEach(function (u) {
            var opt = document.createElement('option');
            opt.value       = u.id;
            opt.textContent = u.username + ' (' + u.rating + ')';
            sel.appendChild(opt);
        });
    } catch (e) {
        console.error('Ошибка загрузки игроков:', e);
    }
}

// Таблица лидеров
async function loadLeaderboard() {
    try {
        var res     = await fetch('/api/Users/leaderboard?top=10');
        var leaders = await res.json();
        var el      = document.getElementById('leaderboard-list');

        if (!leaders.length) {
            el.innerHTML = '<div class="empty-list">Нет данных</div>';
            return;
        }

        var rankClass = ['gold', 'silver', 'bronze'];
        el.innerHTML = leaders.map(function (u, i) {
            return '<div class="leader-row">' +
                '<div class="leader-rank ' + (rankClass[i] || '') + '">' + (i + 1) + '</div>' +
                '<div class="leader-name" onclick="goToProfile(' + u.id + ')">' + escHtml(u.username) + '</div>' +
                '<div class="leader-wins">' + (u.wins || 0) + 'W</div>' +
                '<div class="leader-rating">' + u.rating + '</div>' +
                '</div>';
        }).join('');
    } catch (e) {
        document.getElementById('leaderboard-list').innerHTML =
            '<div class="empty-list">Ошибка загрузки</div>';
    }
}

// Активные игры
async function loadActiveGames() {
    try {
        var res   = await fetch('/api/Games/active?userId=' + currentUser.userId);
        var games = await res.json();
        var el    = document.getElementById('games-list');

        if (!games.length) {
            el.innerHTML = '<div class="empty-list">Нет активных игр.<br>Создайте новую игру выше.</div>';
            document.getElementById('games-count').textContent = '0 игр';
            return;
        }

        document.getElementById('games-count').textContent = games.length + ' игр';
        el.innerHTML = games.map(function (g) {
            var mine = g.isMyGame
                ? '<span class="game-mine">● Ваша</span>'
                : '';
            var statusText = g.status === 'Pending' ? 'Ожидание' : 'Идёт';
            return '<div class="game-row" onclick="joinGameRow(' +
                g.id + ',\'' + escHtml(g.whitePlayer.username) + '\',\'' +
                escHtml(g.blackPlayer.username) + '\',' +
                (g.whitePlayer.id || 0) + ',' + (g.blackPlayer.id || 0) + ',' +
                (g.timeControlMinutes || 10) + ')">' +
                '<div class="game-players">' +
                escHtml(g.whitePlayer.username) + ' <span>vs</span> ' +
                escHtml(g.blackPlayer.username) + mine +
                '</div>' +
                '<span class="game-status ' + g.status + '">' + statusText + '</span>' +
                '</div>';
        }).join('');
    } catch (e) {
        console.error('Ошибка загрузки игр:', e);
    }
}

// Турниры
async function loadTournaments() {
    try {
        var res  = await fetch('/api/Tournaments');
        var list = await res.json();
        var el   = document.getElementById('tournaments-list');

        if (!list || !list.length) {
            el.innerHTML = '<div class="empty-list">Нет турниров.<br>' +
                '<a href="/tournament.html" style="color:var(--accent)">Создать первый →</a></div>';
            return;
        }

        // Показываем максимум 5, остальное — ссылка "все"
        var shown = list.slice(0, 5);
        var statusLabel = { Registration: 'Регистрация', InProgress: 'Идёт', Completed: 'Завершён' };
        var badgeClass  = { Registration: 't-badge-reg', InProgress: 't-badge-live', Completed: 't-badge-done' };

        el.innerHTML = shown.map(function (t) {
            var badge = '<span class="t-badge ' + (badgeClass[t.status] || '') + '">' +
                (statusLabel[t.status] || t.status) + '</span>';
            return '<div class="tournament-row" onclick="window.location.href=\'/tournament.html?id=' + t.id + '\'">' +
                '<span class="tournament-name">' + escHtml(t.name) + '</span>' +
                '<span class="tournament-players">' + (t.participantCount || 0) + ' уч.</span>' +
                badge +
                '</div>';
        }).join('');
    } catch (e) {
        document.getElementById('tournaments-list').innerHTML =
            '<div class="empty-list">Ошибка загрузки</div>';
    }
}

// ── Создать игру ──────────────────────────────────
async function createGame() {
    var opponentId = parseInt(document.getElementById('opponent-select').value);
    var timeMin    = parseInt(document.getElementById('time-select').value);

    if (!opponentId) {
        toast('Выберите противника', 'error');
        return;
    }

    try {
        var res  = await fetch('/api/Games/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                whitePlayerId: currentUser.userId,
                blackPlayerId: opponentId,
                timeControlMinutes: timeMin
            })
        });
        var data = await res.json();

        if (res.ok) {
            var opponentName = document.getElementById('opponent-select')
                .selectedOptions[0].text.split(' (')[0];
            var trainingMode = document.getElementById('training-mode').checked;

            toast('Игра создана!', 'success');
            goToGame(
                data.gameId,
                currentUser.userId,
                opponentId,
                currentUser.username,
                opponentName,
                timeMin,
                trainingMode
            );
        } else {
            toast(data.error || 'Ошибка создания игры', 'error');
        }
    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Подключиться к игре из лобби ─────────────────
function joinGameRow(gameId, whiteName, blackName, whiteId, blackId, timeMin) {
    // timeMin берётся из g.timeControlMinutes который пришёл с сервера — это единственный источник правды
    goToGame(gameId, whiteId, blackId, whiteName, blackName, timeMin || 10);
}