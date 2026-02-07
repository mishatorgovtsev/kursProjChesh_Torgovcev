console.log('=== app.js started ===');

var board = null;
var currentGameId = null;  // ID текущей игры

// === API функции ===

// Создать новую игру
async function createGame(whitePlayerId, blackPlayerId) {
    try {
        const response = await fetch('/api/games/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                whitePlayerId: whitePlayerId,
                blackPlayerId: blackPlayerId
            })
        });

        const data = await response.json();
        currentGameId = data.gameId;
        console.log('Игра создана:', data);
        return data;
    } catch (error) {
        console.error('Ошибка создания игры:', error);
    }
}

// Отправить ход на сервер
async function sendMove(from, to) {
    if (!currentGameId) {
        console.error('Нет активной игры');
        return;
    }

    try {
        const response = await fetch(`/api/games/${currentGameId}/move`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                from: from,
                to: to,
                color: board.orientation() === 'white' ? 'w' : 'b'
            })
        });

        // Проверяем статус ответа
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Ошибка сервера:', errorText);
            // Возвращаем фигуру назад
            board.position(board.position(), false);
            return;
        }

        const data = await response.json();
        console.log('Ответ сервера:', data);

        if (data.success) {
            updateTurn();
            if (data.isGameOver) {
                alert('Игра окончена!');
            }
        } else {
            console.error('Недопустимый ход');
            // Возвращаем доску в предыдущее состояние
            board.position(data.newFen || board.position(), false);
        }

        return data;
    } catch (error) {
        console.error('Ошибка отправки хода:', error);
        // При ошибке сети тоже возвращаем фигуру
        board.position(board.position(), false);
    }
}

// Получить состояние игры
async function getGame(gameId) {
    try {
        const response = await fetch(`/api/games/${gameId}`);
        const data = await response.json();
        console.log('Состояние игры:', data);

        if (data.currentFEN) {
            board.position(data.currentFEN);
        }

        return data;
    } catch (error) {
        console.error('Ошибка получения игры:', error);
    }
}

// === Инициализация ===

$(document).ready(function() {
    console.log('=== document ready ===');

    // Создаём доску
    board = Chessboard('board', {
        position: 'start',
        draggable: true,
        dropOffBoard: 'snapback',
        pieceTheme: 'https://chessboardjs.com/img/chesspieces/wikipedia/{piece}.png',
        onDrop: onDrop  // Обработчик хода
    });

    console.log('Board created successfully:', board);

    // Автоматически создаём тестовую игру (временно)
    // В реальности здесь будет вход в систему и выбор соперника
    createGame(1, 2);  // Тестовые ID игроков
});

// Обработка хода
function onDrop(source, target, piece, newPos, oldPos, orientation) {
    console.log('Ход:', source, '->', target);

    // Нельзя ходить на ту же клетку
    if (source === target) return 'snapback';

    // Отправляем на сервер
    sendMove(source, target);

    // Пока сервер не ответил, разрешаем ход (оптимистично)
    // Если сервер вернёт ошибку — доска откатится
}

// Смена хода (визуально)
function updateTurn() {
    var turnSpan = document.getElementById('turn');
    if (turnSpan) {
        turnSpan.textContent = turnSpan.textContent === 'Белые' ? 'Чёрные' : 'Белые';
    }
}

// Новая игра
function resetBoard() {
    console.log('resetBoard called');
    board.start();
    document.getElementById('turn').textContent = 'Белые';
    createGame(1, 2);  // Создаём новую игру
}

console.log('=== app.js finished ===');