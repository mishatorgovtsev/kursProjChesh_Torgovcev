// ═══════════════════════════════════════════════════
//  Chess Platform — app.js
// ═══════════════════════════════════════════════════

// ── Глобальное состояние ─────────────────────────────
var currentUser   = null;
var currentGameId = null;
var board         = null;
var game          = null;   // chess.js instance для валидации ходов
var isProcessing  = false;
    
var pendingPromotion = null; // { source, target }
var hubConnection = null;
var myColor = 'w'; // цвет текущего игрока

// Таймеры
var whiteTimeSec  = 600;
var blackTimeSec  = 600;
var timerInterval = null;
var currentTurn   = 'w';

// История ходов для отображения
var moveHistory   = [];

// ── Toast ─────────────────────────────────────────────
function toast(msg, type) {
    var el = document.getElementById('toast');
    el.textContent = msg;
    el.className = 'toast ' + (type || 'success') + ' show';
    setTimeout(function() { el.className = 'toast'; }, 3000);
}

// ── Auth Tab ─────────────────────────────────────────
function switchAuthTab(tab) {
    document.querySelectorAll('.auth-tab').forEach(function(t, i) {
        t.classList.toggle('active', (i === 0) === (tab === 'login'));
    });
    document.getElementById('login-form').classList.toggle('active', tab === 'login');
    document.getElementById('register-form').classList.toggle('active', tab === 'register');
    document.getElementById('login-error').textContent = '';
    document.getElementById('reg-error').textContent = '';
}

// ── Регистрация ───────────────────────────────────────
async function register() {
    if (isProcessing) return;
    var btn = document.getElementById('reg-btn');
    var errEl = document.getElementById('reg-error');
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
        var res = await fetch('/api/Users/register', {
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
    } catch(e) {
        errEl.textContent = 'Ошибка сети';
    } finally {
        btn.disabled = false;
        isProcessing = false;
        btn.textContent = 'Зарегистрироваться';
    }
}

// ── Вход ─────────────────────────────────────────────
async function login() {
    if (isProcessing) return;
    var btn = document.getElementById('login-btn');
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
        var res = await fetch('/api/Users/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        var data = await res.json();

        if (res.ok) {
            currentUser = data;
            localStorage.setItem('chess_user', JSON.stringify(data));
            showLobby();
        } else {
            errEl.textContent = data.error || 'Неверные данные';
        }
    } catch(e) {
        errEl.textContent = 'Ошибка сети';
    } finally {
        btn.disabled = false;
        isProcessing = false;
        btn.textContent = 'Войти';
    }
}

// ── Выход ─────────────────────────────────────────────
function logout() {
    currentUser = null;
    localStorage.removeItem('chess_user');
    stopTimer();
    showScreen('auth-screen');
    document.getElementById('main-header').style.display = 'none';
}

// ── Экраны ───────────────────────────────────────────
function showScreen(id) {
    document.querySelectorAll('.screen').forEach(function(s) {
        s.classList.remove('active');
    });
    // auth-screen не имеет класса screen
    document.getElementById('auth-screen').style.display = 'none';
    document.getElementById('lobby-screen').classList.remove('active');
    document.getElementById('game-screen').classList.remove('active');

    if (id === 'auth-screen') {
        document.getElementById('auth-screen').style.display = 'flex';
    } else {
        document.getElementById(id).classList.add('active');
    }
}

// ── Лобби ─────────────────────────────────────────────
async function showLobby() {
    showScreen('lobby-screen');
    document.getElementById('main-header').style.display = 'flex';
    document.getElementById('header-user').innerHTML =
        '<strong>' + currentUser.username + '</strong> (' + currentUser.rating + ')';
    await loadLobby();
}

async function loadLobby() {
    await Promise.all([loadOpponents(), loadLeaderboard(), loadActiveGames()]);
}

// Загружаем список игроков для выбора противника
async function loadOpponents() {
    try {
        var res = await fetch('/api/Users/leaderboard?top=50');
        var users = await res.json();

        var select = document.getElementById('opponent-select');
        select.innerHTML = '';

        var others = users.filter(function(u) { return u.id !== currentUser.userId; });

        if (others.length === 0) {
            select.innerHTML = '<option value="">Нет других игроков</option>';
            return;
        }

        others.forEach(function(u) {
            var opt = document.createElement('option');
            opt.value = u.id;
            opt.textContent = u.username + ' (' + u.rating + ')';
            select.appendChild(opt);
        });
    } catch(e) {
        console.error('Ошибка загрузки игроков:', e);
    }
}

// Загружаем таблицу лидеров
async function loadLeaderboard() {
    try {
        var res = await fetch('/api/Users/leaderboard?top=10');
        var leaders = await res.json();
        var el = document.getElementById('leaderboard-list');

        if (leaders.length === 0) {
            el.innerHTML = '<div class="empty-list">Нет данных</div>';
            return;
        }

        var rankClass = ['gold', 'silver', 'bronze'];
        el.innerHTML = leaders.map(function(u, i) {
            return '<div class="leader-row">' +
                '<div class="leader-rank ' + (rankClass[i] || '') + '">' + (i + 1) + '</div>' +
                '<div class="leader-name">' + escHtml(u.username) + '</div>' +
                '<div class="leader-wins">' + (u.wins || 0) + 'W</div>' +
                '<div class="leader-rating">' + u.rating + '</div>' +
                '</div>';
        }).join('');
    } catch(e) {
        document.getElementById('leaderboard-list').innerHTML =
            '<div class="empty-list">Ошибка загрузки</div>';
    }
}

// Загружаем активные игры (используем leaderboard как proxy для демонстрации)
async function loadActiveGames() {
    try {
        var res = await fetch('/api/Games/active?userId=' + currentUser.userId);
        var games = await res.json();
        var el = document.getElementById('games-list');

        if (games.length === 0) {
            el.innerHTML = '<div class="empty-list">Нет активных игр.<br>Создайте новую игру выше.</div>';
            document.getElementById('games-count').textContent = '0 игр';
            return;
        }

        document.getElementById('games-count').textContent = games.length + ' игр';
        el.innerHTML = games.map(function(g) {
            var isMyGame = g.isMyGame;
            var label = isMyGame ? ' <span style="color:var(--accent);font-size:11px">● Ваша</span>' : '';
            return '<div class="game-row" onclick="joinGameFromLobby(' + g.id + ',\'' +
                g.whitePlayer.username + '\',\'' + g.blackPlayer.username + '\',' + g.timeControlMinutes + ')">' +
                '<div class="game-players">' + g.whitePlayer.username +
                ' <span>vs</span> ' + g.blackPlayer.username + label + '</div>' +
                '<span class="game-status ' + g.status + '">' +
                (g.status === 'Pending' ? 'Ожидание' : 'Идёт') + '</span>' +
                '</div>';
        }).join('');
    } catch(e) {
        console.error('Ошибка загрузки игр:', e);
    }
}

function joinGameFromLobby(gameId, whiteName, blackName, timeMin) {
    // Определяем наш цвет
    var color = 'b'; // по умолчанию чёрные (присоединяемся к чужой игре)
    joinExistingGame(gameId, whiteName, blackName, timeMin, color);
}

// ── Создать игру ──────────────────────────────────────
async function createGame() {
    var opponentId = parseInt(document.getElementById('opponent-select').value);
    var timeMin    = parseInt(document.getElementById('time-select').value);

    if (!opponentId) {
        toast('Выберите противника', 'error');
        return;
    }

    try {
        var res = await fetch('/api/Games/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                whitePlayerId: currentUser.userId,
                blackPlayerId: opponentId
            })
        });

        var data = await res.json();

        if (res.ok) {
            toast('Игра создана!', 'success');
            // Загружаем противника для отображения имени
            var opponentName = document.getElementById('opponent-select')
                .selectedOptions[0].text.split(' (')[0];
            startGameScreen(data.gameId, currentUser.username, opponentName, timeMin, 'w');
        } else {
            toast(data.error || 'Ошибка создания игры', 'error');
        }
    } catch(e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Игровой экран ─────────────────────────────────────
function startGameScreen(gameId, whiteName, blackName, timeMin, color) {
    myColor = color || 'w';
    currentGameId = gameId;
    moveHistory   = [];
    currentTurn   = 'w';

    var timeSec = (timeMin || 10) * 60;
    whiteTimeSec = timeSec;
    blackTimeSec = timeSec;

    // Имена игроков
    document.getElementById('white-player-name').textContent = whiteName + ' (Вы)';
    document.getElementById('black-player-name').textContent = blackName;

    // Сбросить историю
    document.getElementById('moves-list').innerHTML =
        '<span style="color:var(--text-muted);font-size:13px">Ходов ещё нет</span>';

    updateTimerDisplay();
    showScreen('game-screen');
    initBoard();
    startTimer();
    connectToHub(gameId);
}

// ── Доска и chess.js ──────────────────────────────────
function initBoard() {
    if (typeof Chess !== 'undefined') {
        game = new Chess();
    } else {
        game = null;
    }

    if (board) board.destroy();

    // Определяем ориентацию — если играем за чёрных, переворачиваем доску
    var orientation = myColor === 'b' ? 'black' : 'white';

    board = Chessboard('board', {
        position: 'start',
        draggable: true,
        dropOffBoard: 'snapback',
        pieceTheme: 'https://chessboardjs.com/img/chesspieces/wikipedia/{piece}.png',
        orientation: orientation,
        onDragStart: onDragStart,
        onDrop: onDrop,
        onSnapEnd: onSnapEnd
    });
}
// Запрет перетаскивания не в свой ход
function onDragStart(source, piece) {
    if (game && game.game_over()) return false;

    var trainingMode = document.getElementById('training-mode') &&
        document.getElementById('training-mode').checked;
    if (trainingMode) return true;

    // Блокируем если не наш ход
    if (currentTurn !== myColor) return false;

    // Блокируем чужие фигуры
    if (myColor === 'w' && piece.search(/^b/) !== -1) return false;
    if (myColor === 'b' && piece.search(/^w/) !== -1) return false;
}

async function onDrop(source, target) {
    if (source === target) return 'snapback';
    if (isProcessing) return 'snapback';

    // Проверяем валидность через chess.js
    if (game) {
        var testMove = game.move({ from: source, to: target, promotion: 'q' });
        if (testMove === null) return 'snapback';
        game.undo();

        // Проверяем — это превращение пешки?
        var piece = game.get(source);
        var isPromotion = piece && piece.type === 'p' &&
            ((piece.color === 'w' && target[1] === '8') ||
                (piece.color === 'b' && target[1] === '1'));

        if (isPromotion) {
            pendingPromotion = { source, target };
            document.getElementById('promotion-modal').classList.add('open');
            return 'snapback'; // Ждём выбора фигуры
        }
    }

    await sendMove(source, target, 'q');
}

function connectToHub(gameId) {
    if (hubConnection) hubConnection.stop();

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/gameHub')
        .withAutomaticReconnect()
        .build();

    hubConnection.on('MoveMade', function(data) {
        if (data.sentByUserId === currentUser.userId) return;

        if (game) {
            var result = game.move({ from: data.from, to: data.to, promotion: data.promotion || 'q' });
            if (!result) {
                // chess.js отклонил — загружаем FEN напрямую с сервера
                game.load(data.newFen);
            }
        }
        board.position(game ? game.fen() : data.newFen);

        // Определяем чей был ход по nextTurn: если nextTurn='b', значит только что ходили белые
        var movedColor = data.nextTurn === 'b' ? 'w' : 'b';
        addMoveToHistory(data.from, data.to, movedColor);

        currentTurn = data.nextTurn;
        document.getElementById('turn-name').textContent = currentTurn === 'w' ? 'Белые' : 'Чёрные';
        updateTimerDisplay();

        if (data.isGameOver) {
            stopTimer();
            showGameOver('Игра окончена!', 'Мат.');
        }
    });

    hubConnection.on('MoveUndone', function(data) {
        if (game) { game.undo(); board.position(game.fen()); }
        else board.position(data.fen);
    });

    hubConnection.on('PlayerJoined', function() { toast('Противник подключился!', 'success'); });
    hubConnection.on('PlayerLeft',  function() { toast('Противник отключился', 'error'); });

    hubConnection.start()
        .then(function() { return hubConnection.invoke('JoinGame', gameId); })
        .catch(function(e) { console.error('SignalR:', e); });
}

async function selectPromotion(piece) {
    document.getElementById('promotion-modal').classList.remove('open');
    if (pendingPromotion) {
        await sendMove(pendingPromotion.source, pendingPromotion.target, piece);
        pendingPromotion = null;
    }
}

async function sendMove(source, target, promotion) {
    isProcessing = true;
    try {
        var res = await fetch('/api/Games/' + currentGameId + '/move', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                from: source,
                to: target,
                color: myColor === 'w' ? 'white' : 'black',  // было currentTurn
                promotion: promotion,
                userId: currentUser.userId
            })
        });

        var data = await res.json();
        if (!data.success) { isProcessing = false; return; }

        if (game) game.move({ from: source, to: target, promotion: promotion });
        board.position(game ? game.fen() : data.newFen);

        addMoveToHistory(source, target, currentTurn);
        switchTurn();

        if (data.isGameOver) {
            stopTimer();
            // Определяем победителя
            var winnerId = myColor === 'w' ? currentUser.userId : null;
            var result = myColor === 'w' ? 'white' : 'black';

            // Отправляем результат на сервер
            fetch('/api/Games/' + currentGameId + '/finish', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ result: result, winnerId: winnerId })
            });

            showGameOver('Игра окончена!', 'Мат. Победили ' + (myColor === 'w' ? 'белые' : 'чёрные'));
        }
    } catch(e) {
        console.error('Ошибка хода:', e);
    }
    isProcessing = false;
}

function onSnapEnd() {
    if (game) board.position(game.fen());
}

// ── История ходов ─────────────────────────────────────
function addMoveToHistory(from, to, color) {
    var notation = from + '-' + to;
    if (color === 'w') {
        moveHistory.push({ w: notation, b: null });
    } else {
        if (moveHistory.length > 0) {
            moveHistory[moveHistory.length - 1].b = notation;
        }
    }
    renderMoveHistory();
}

function renderMoveHistory() {
    var el = document.getElementById('moves-list');
    if (moveHistory.length === 0) {
        el.innerHTML = '<span style="color:var(--text-muted);font-size:13px">Ходов ещё нет</span>';
        return;
    }
    el.innerHTML = moveHistory.map(function(pair, i) {
        return '<div class="move-pair">' +
            '<span class="move-num">' + (i + 1) + '.</span>' +
            '<span class="move-w">' + (pair.w || '') + '</span>' +
            '<span class="move-b">' + (pair.b || '') + '</span>' +
            '</div>';
    }).join('');
    el.scrollTop = el.scrollHeight;
}

// ── Таймеры ───────────────────────────────────────────
function formatTime(sec) {
    var m = Math.floor(sec / 60);
    var s = sec % 60;
    return m.toString().padStart(2, '0') + ':' + s.toString().padStart(2, '0');
}

function updateTimerDisplay() {
    var wEl = document.getElementById('white-timer');
    var bEl = document.getElementById('black-timer');

    wEl.textContent = formatTime(whiteTimeSec);
    bEl.textContent = formatTime(blackTimeSec);

    wEl.className = 'timer-display' + (currentTurn === 'w' ? ' active' : '') + (whiteTimeSec < 30 ? ' low' : '');
    bEl.className = 'timer-display' + (currentTurn === 'b' ? ' active' : '') + (blackTimeSec < 30 ? ' low' : '');
}

function startTimer() {
    stopTimer();
    timerInterval = setInterval(function() {
        if (currentTurn === 'w') {
            whiteTimeSec--;
            if (whiteTimeSec <= 0) {
                stopTimer();
                fetch('/api/Games/' + currentGameId + '/finish', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ result: 'black', winnerId: null })
                });
                showGameOver('Время истекло!', 'Время белых истекло. Победили чёрные.');
                return;
            }
        } else {
            blackTimeSec--;
            if (blackTimeSec <= 0) {
                stopTimer();
                fetch('/api/Games/' + currentGameId + '/finish', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ result: 'white', winnerId: null })
                });
                showGameOver('Время истекло!', 'Время чёрных истекло. Победили белые.');
                return;
            }
        }
        updateTimerDisplay();
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

function switchTurn() {
    currentTurn = currentTurn === 'w' ? 'b' : 'w';
    document.getElementById('turn-name').textContent = currentTurn === 'w' ? 'Белые' : 'Чёрные';
    updateTimerDisplay();
}

// ── Game Over ─────────────────────────────────────────
function showGameOver(title, text) {
    document.getElementById('gameover-title').textContent = title;
    document.getElementById('gameover-text').textContent = text;
    document.getElementById('gameover-modal').classList.add('open');
}

// ── Навигация ─────────────────────────────────────────
function goToLobby() {
    stopTimer();
    document.getElementById('gameover-modal').classList.remove('open');
    showLobby();
}

async function undoMove() {
    if (!currentGameId) return;
    try {
        var res = await fetch('/api/Games/' + currentGameId + '/undo', {
            method: 'POST'
        });
        var data = await res.json();
        if (data.success) {
            // Откатываем chess.js
            if (game) game.undo();
            board.position(game ? game.fen() : data.fen);
            // Убираем последний ход из истории
            if (moveHistory.length > 0) {
                var last = moveHistory[moveHistory.length - 1];
                if (last.b) { last.b = null; currentTurn = 'b'; }
                else { moveHistory.pop(); currentTurn = 'w'; }
                renderMoveHistory();
            }
            document.getElementById('turn-name').textContent = currentTurn === 'w' ? 'Белые' : 'Чёрные';
            updateTimerDisplay();
            toast('Ход отменён', 'success');
        } else {
            toast(data.error || 'Нет ходов для отмены', 'error');
        }
    } catch(e) {
        toast('Ошибка', 'error');
    }
}
async function joinExistingGame(gameId, whiteName, blackName, timeMin, color) {
    currentGameId = gameId;  // устанавливаем gameId БЕЗ создания новой игры
    moveHistory = [];
    currentTurn = 'w';
    myColor = color || 'b';

    var timeSec = (timeMin || 10) * 60;
    whiteTimeSec = timeSec;
    blackTimeSec = timeSec;

    document.getElementById('white-player-name').textContent = whiteName;
    document.getElementById('black-player-name').textContent = blackName + ' (Вы)';
    document.getElementById('moves-list').innerHTML =
        '<span style="color:var(--text-muted);font-size:13px">Ходов ещё нет</span>';

    updateTimerDisplay();
    showScreen('game-screen');
    initBoard();
    startTimer();
    connectToHub(gameId);
}

// ── Утилиты ───────────────────────────────────────────
function escHtml(str) {
    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}

// ── Инициализация ─────────────────────────────────────
document.addEventListener('DOMContentLoaded', function() {
    var saved = localStorage.getItem('chess_user');
    if (saved) {
        try {
            currentUser = JSON.parse(saved);
            showLobby();
        } catch(e) {
            localStorage.removeItem('chess_user');
            showScreen('auth-screen');
        }
    } else {
        showScreen('auth-screen');
    }
}); 