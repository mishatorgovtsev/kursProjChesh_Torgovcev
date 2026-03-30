// ═══════════════════════════════════════════════════
//  Chess Platform — game.js
//  Логика игровой страницы (game.html)
// ═══════════════════════════════════════════════════

// ── Состояние игры ────────────────────────────────
var currentGameId    = null;
var board            = null;
var game             = null;   // chess.js
var isProcessing     = false;
var pendingPromotion = null;   // { source, target }
var hubConnection    = null;
var myColor          = 'w';

var whiteTimeSec  = 600;
var blackTimeSec  = 600;
var timerInterval = null;
var currentTurn   = 'w';

var moveHistory = [];

// Данные игры из sessionStorage
var gameData = null;

// ── Инициализация ─────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var user = loadCurrentUser();
    if (!user) {
        window.location.href = '/';
        return;
    }

    initHeader();

    // Загружаем параметры игры из sessionStorage
    var raw = sessionStorage.getItem('chess_game');
    if (!raw) {
        toast('Игра не найдена', 'error');
        setTimeout(function () { window.location.href = '/'; }, 1500);
        return;
    }

    try {
        gameData = JSON.parse(raw);
    } catch (e) {
        window.location.href = '/';
        return;
    }

    // Определяем цвет текущего игрока — parseInt чтобы избежать '5' === 5 → false
    myColor = (parseInt(gameData.whiteId) === parseInt(currentUser.userId)) ? 'w' : 'b';

    startGame();
});

// ── Запуск игры ───────────────────────────────────
function startGame() {
    currentGameId = gameData.gameId;
    moveHistory   = [];
    currentTurn   = 'w';

    var timeSec  = (gameData.timeMin || 10) * 60;
    whiteTimeSec = timeSec;
    blackTimeSec = timeSec;

    var whiteName = gameData.whiteName || 'Белые';
    var blackName = gameData.blackName || 'Чёрные';

    var isWhite = myColor === 'w';

    // Ориентация доски:
    //   Белые играют снизу → верхний бар = противник (чёрные), нижний = ты (белые)
    //   Чёрные играют снизу → верхний бар = противник (белые), нижний = ты (чёрные)
    //
    // Элементы в HTML:
    //   #top-player-link    / #top-timer    — верхний бар (всегда противник)
    //   #bottom-player-link / #bottom-timer — нижний бар  (всегда ты)

    var myName       = isWhite ? whiteName : blackName;
    var opponentName = isWhite ? blackName : whiteName;
    var myId         = isWhite ? gameData.whiteId : gameData.blackId;
    var opponentId   = isWhite ? gameData.blackId : gameData.whiteId;

    // Верхний бар — противник
    document.getElementById('top-player-link').textContent = opponentName;
    document.getElementById('top-player-link').onclick = function () {
        if (opponentId) goToProfile(opponentId);
    };

    // Нижний бар — я
    document.getElementById('bottom-player-link').textContent = myName + ' (Вы)';
    document.getElementById('bottom-player-link').onclick = function () {
        if (myId) goToProfile(myId);
    };

    // Таймеры — храним по цвету (white/black), показываем по позиции (top/bottom)
    updateTimerDisplay();

    // Панель игроков справа — всегда белые сверху, чёрные снизу
    var sideWhite = document.getElementById('side-white-name');
    var sideBlack = document.getElementById('side-black-name');
    sideWhite.textContent = whiteName;
    sideBlack.textContent = blackName;
    if (isWhite) sideWhite.classList.add('you');
    else         sideBlack.classList.add('you');
    sideWhite.onclick = function () { if (gameData.whiteId) goToProfile(gameData.whiteId); };
    sideBlack.onclick = function () { if (gameData.blackId) goToProfile(gameData.blackId); };

    // История ходов — очистить
    document.getElementById('moves-list').innerHTML =
        '<span style="color:var(--text-muted);font-size:13px">Ходов ещё нет</span>';

    initBoard();
    startTimer();
    connectToHub(currentGameId);
}

// ── Доска ─────────────────────────────────────────
function initBoard() {
    game = (typeof Chess !== 'undefined') ? new Chess() : null;

    if (board) board.destroy();

    var trainingMode = document.getElementById('training-mode');
    var orientation  = myColor === 'b' ? 'black' : 'white';

    board = Chessboard('board', {
        position:    'start',
        draggable:   true,
        dropOffBoard:'snapback',
        pieceTheme:  'https://chessboardjs.com/img/chesspieces/wikipedia/{piece}.png',
        orientation: orientation,
        onDragStart: onDragStart,
        onDrop:      onDrop,
        onSnapEnd:   onSnapEnd
    });
}

function onDragStart(source, piece) {
    if (game && game.game_over()) return false;

    // В тренировочном режиме разрешаем ходить за обоих
    // (training-mode чекбокса нет на game.html, он был в лобби — передаём через sessionStorage)
    if (gameData && gameData.trainingMode) return true;

    if (currentTurn !== myColor) return false;
    if (myColor === 'w' && piece.search(/^b/) !== -1) return false;
    if (myColor === 'b' && piece.search(/^w/) !== -1) return false;
}

async function onDrop(source, target) {
    if (source === target) return 'snapback';
    if (isProcessing)      return 'snapback';

    if (game) {
        var testMove = game.move({ from: source, to: target, promotion: 'q' });
        if (testMove === null) return 'snapback';
        game.undo();

        var piece       = game.get(source);
        var isPromotion = piece && piece.type === 'p' &&
            ((piece.color === 'w' && target[1] === '8') ||
                (piece.color === 'b' && target[1] === '1'));

        if (isPromotion) {
            pendingPromotion = { source, target };
            document.getElementById('promotion-modal').classList.add('open');
            return 'snapback';
        }
    }

    await sendMove(source, target, 'q');
}

function onSnapEnd() {
    if (game) board.position(game.fen());
}

// ── SignalR ───────────────────────────────────────
function connectToHub(gameId) {
    if (hubConnection) hubConnection.stop();

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/gameHub')
        .withAutomaticReconnect()
        .build();

    hubConnection.on('MoveMade', function (data) {
        if (game) {
            var result = game.move({ from: data.from, to: data.to, promotion: data.promotion || 'q' });
            if (!result) game.load(data.newFen);
        }
        board.position(game ? game.fen() : data.newFen);

        var movedColor = data.nextTurn === 'b' ? 'w' : 'b';
        addMoveToHistory(data.from, data.to, movedColor);

        // Синхронизируем время если сервер прислал остаток
        if (typeof data.whiteTimeSec === 'number') whiteTimeSec = data.whiteTimeSec;
        if (typeof data.blackTimeSec === 'number') blackTimeSec = data.blackTimeSec;

        // switchTurn переключает currentTurn и перезапускает отображение у ВСЕХ
        currentTurn = data.nextTurn;
        document.getElementById('turn-name').textContent = currentTurn === 'w' ? 'Белые' : 'Чёрные';
        updateTimerDisplay();

        if (data.isGameOver) {
            stopTimer();
            showGameOver('Игра окончена!', 'Мат.');
        }
    });

    hubConnection.on('MoveUndone', function (data) {
        if (game) { game.undo(); board.position(game.fen()); }
        else board.position(data.fen);
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
    });

    hubConnection.on('PlayerJoined', function () { toast('Противник подключился!', 'success'); });
    hubConnection.on('PlayerLeft',   function () { toast('Противник отключился', 'error'); });

    // Противник предлагает ничью
    hubConnection.on('DrawOffered', function () {
        document.getElementById('draw-offer-banner').classList.add('visible');
    });

    // Противник отклонил ничью
    hubConnection.on('DrawDeclined', function () {
        toast('Противник отклонил ничью', 'error');
    });

    // Игра завершена (сервер прислал после /finish)
    hubConnection.on('GameFinished', function (data) {
        stopTimer();
        var isWhite  = myColor === 'w';
        var iWon     = (data.result === 'white' && isWhite) || (data.result === 'black' && !isWhite);
        var isDraw   = data.result === 'draw';

        var title = isDraw ? 'Ничья!' : (iWon ? '🏆 Победа!' : '😞 Поражение');
        var myRating = isWhite ? data.whiteRating : data.blackRating;
        var ratingChange = myRating ? (myRating.newR - myRating.old) : 0;
        var sign  = ratingChange >= 0 ? '+' : '';
        var text  = isDraw
            ? 'Партия завершилась вничью.'
            : (iWon ? 'Вы победили!' : 'Вы проиграли.');
        if (myRating) text += '  Рейтинг: ' + myRating.old + ' → ' + myRating.newR +
            ' (' + sign + ratingChange + ')';

        // Обновляем рейтинг в localStorage
        if (myRating && currentUser) {
            currentUser.rating = myRating.newR;
            saveCurrentUser(currentUser);
            initHeader();
        }

        showGameOver(title, text);
    });

    hubConnection.start()
        .then(function () { return hubConnection.invoke('JoinGame', gameId); })
        .catch(function (e) { console.error('SignalR:', e); });
}

// ── Ходы ─────────────────────────────────────────
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
                from:      source,
                to:        target,
                color:     myColor === 'w' ? 'white' : 'black',
                promotion: promotion,
                userId:    currentUser.userId
            })
        });

        var data = await res.json();
        if (!data.success) { isProcessing = false; return; }

        if (game) game.move({ from: source, to: target, promotion: promotion });
        board.position(game ? game.fen() : data.newFen);

        // НЕ вызываем switchTurn здесь — таймер и ход переключатся через MoveMade
        // (SignalR рассылает MoveMade всем включая отправителя)

        if (data.isGameOver) {
            stopTimer();
            var result = myColor === 'w' ? 'white' : 'black';
            fetch('/api/Games/' + currentGameId + '/finish', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ result: result, winnerId: currentUser.userId })
            });
            showGameOver('Игра окончена!',
                'Мат. Победили ' + (myColor === 'w' ? 'белые' : 'чёрные'));
        }
    } catch (e) {
        console.error('Ошибка хода:', e);
    }
    isProcessing = false;
}

async function undoMove() {
    if (!currentGameId) return;
    try {
        var res  = await fetch('/api/Games/' + currentGameId + '/undo', { method: 'POST' });
        var data = await res.json();
        if (data.success) {
            if (game) game.undo();
            board.position(game ? game.fen() : data.fen);
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
    } catch (e) {
        toast('Ошибка', 'error');
    }
}

// ── История ходов ─────────────────────────────────
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
    if (!moveHistory.length) {
        el.innerHTML = '<span style="color:var(--text-muted);font-size:13px">Ходов ещё нет</span>';
        return;
    }
    el.innerHTML = moveHistory.map(function (pair, i) {
        return '<div class="move-pair">' +
            '<span class="move-num">' + (i + 1) + '.</span>' +
            '<span class="move-w">' + (pair.w || '') + '</span>' +
            '<span class="move-b">' + (pair.b || '') + '</span>' +
            '</div>';
    }).join('');
    el.scrollTop = el.scrollHeight;
}

// ── Таймеры ───────────────────────────────────────
function updateTimerDisplay() {
    var topEl    = document.getElementById('top-timer');
    var bottomEl = document.getElementById('bottom-timer');

    var isWhite = myColor === 'w';

    // Верхний таймер — противник
    var topSec    = isWhite ? blackTimeSec : whiteTimeSec;
    // Нижний таймер — я
    var bottomSec = isWhite ? whiteTimeSec : blackTimeSec;

    // Чей сейчас ход? Активен тот чья очередь
    var myTurn       = (currentTurn === 'w' && isWhite) || (currentTurn === 'b' && !isWhite);
    var opponentTurn = !myTurn;

    topEl.textContent    = formatTime(topSec);
    bottomEl.textContent = formatTime(bottomSec);

    topEl.className    = 'timer-display' + (opponentTurn ? ' active' : '') + (topSec    < 30 ? ' low' : '');
    bottomEl.className = 'timer-display' + (myTurn       ? ' active' : '') + (bottomSec < 30 ? ' low' : '');
}

function startTimer() {
    stopTimer();
    timerInterval = setInterval(function () {
        if (currentTurn === 'w') {
            whiteTimeSec--;
            if (whiteTimeSec <= 0) {
                stopTimer();
                finishByTimeout('black');
                showGameOver('Время истекло!', 'Время белых истекло. Победили чёрные.');
                return;
            }
        } else {
            blackTimeSec--;
            if (blackTimeSec <= 0) {
                stopTimer();
                finishByTimeout('white');
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

function finishByTimeout(winner) {
    fetch('/api/Games/' + currentGameId + '/finish', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ result: winner, winnerId: null })
    });
}

function switchTurn() {
    currentTurn = currentTurn === 'w' ? 'b' : 'w';
    document.getElementById('turn-name').textContent = currentTurn === 'w' ? 'Белые' : 'Чёрные';
    updateTimerDisplay();
}

// ── Game Over ─────────────────────────────────────
function showGameOver(title, text) {
    document.getElementById('gameover-title').textContent = title;
    document.getElementById('gameover-text').textContent  = text;
    document.getElementById('gameover-modal').classList.add('open');
}

// ── Сдача ─────────────────────────────────────────
function confirmResign() {
    document.getElementById('resign-modal').classList.add('open');
}

function closeResignModal() {
    document.getElementById('resign-modal').classList.remove('open');
}

async function doResign() {
    closeResignModal();
    stopTimer();

    var winner    = myColor === 'w' ? 'black' : 'white';
    var winnerId  = myColor === 'w' ? gameData.blackId : gameData.whiteId;

    try {
        await fetch('/api/Games/' + currentGameId + '/finish', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result: winner, winnerId: parseInt(winnerId) })
        });
        // GameFinished придёт через SignalR всем включая нас
    } catch (e) {
        toast('Ошибка при сдаче', 'error');
    }
}

// ── Ничья ──────────────────────────────────────────
async function offerDraw() {
    if (!hubConnection) return;
    try {
        await hubConnection.invoke('OfferDraw', currentGameId);
        toast('Предложение ничьей отправлено', 'success');
    } catch (e) {
        toast('Ошибка отправки предложения', 'error');
    }
}

async function acceptDraw() {
    document.getElementById('draw-offer-banner').classList.remove('visible');
    stopTimer();

    try {
        await fetch('/api/Games/' + currentGameId + '/finish', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result: 'draw', winnerId: null })
        });
        // GameFinished придёт через SignalR
    } catch (e) {
        toast('Ошибка при принятии ничьей', 'error');
    }
}

async function declineDraw() {
    document.getElementById('draw-offer-banner').classList.remove('visible');
    if (hubConnection) {
        try {
            await hubConnection.invoke('DeclineDraw', currentGameId);
        } catch (e) { /* Hub метод опциональный */ }
    }
    toast('Вы отклонили ничью', 'success');
}