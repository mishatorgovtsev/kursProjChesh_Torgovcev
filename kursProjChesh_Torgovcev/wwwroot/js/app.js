// === Глобальные переменные ===
var board = null;
var currentGameId = null;
var currentUser = null;
var isProcessing = false; // Флаг блокировки

// === API функции ===

// Регистрация
async function register() {
    if (isProcessing) return; // Если уже обрабатывается - выходим

    const btn = document.querySelector('#register-form button');
    btn.disabled = true;
    isProcessing = true;

    const username = document.getElementById('reg-username').value;
    const email = document.getElementById('reg-email').value;
    const password = document.getElementById('reg-password').value;

    // Простая валидация
    if (!username || !email || !password) {
        document.getElementById('reg-error').textContent = 'Заполните все поля';
        btn.disabled = false;
        isProcessing = false;
        return;
    }

    try {
        const response = await fetch('/api/Users/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });

        const data = await response.json();

        if (response.ok) {
            alert('Регистрация успешна! Войдите в систему.');
            showLogin();
            // Очищаем поля
            document.getElementById('reg-username').value = '';
            document.getElementById('reg-email').value = '';
            document.getElementById('reg-password').value = '';
            document.getElementById('reg-error').textContent = '';
        } else {
            document.getElementById('reg-error').textContent = data.error || 'Ошибка регистрации';
        }
    } catch (error) {
        document.getElementById('reg-error').textContent = 'Ошибка сети';
    } finally {
        btn.disabled = false;
        isProcessing = false;
    }
}

// Вход
async function login() {
    if (isProcessing) return;

    const btn = document.querySelector('#login-form button');
    btn.disabled = true;
    isProcessing = true;

    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;

    if (!username || !password) {
        document.getElementById('login-error').textContent = 'Введите имя пользователя и пароль';
        btn.disabled = false;
        isProcessing = false;
        return;
    }

    try {
        const response = await fetch('/api/Users/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (response.ok) {
            currentUser = data;
            localStorage.setItem('user', JSON.stringify(data));
            showGameSection();
        } else {
            document.getElementById('login-error').textContent = data.error || 'Неверные данные';
        }
    } catch (error) {
        document.getElementById('login-error').textContent = 'Ошибка сети';
    } finally {
        btn.disabled = false;
        isProcessing = false;
    }
}

// Выход
function logout() {
    currentUser = null;
    localStorage.removeItem('user');
    showAuthSection();
}

// === UI функции ===

function showLogin() {
    document.getElementById('login-form').style.display = 'block';
    document.getElementById('register-form').style.display = 'none';
    document.getElementById('login-error').textContent = '';
    isProcessing = false;
}

function showRegister() {
    document.getElementById('login-form').style.display = 'none';
    document.getElementById('register-form').style.display = 'block';
    document.getElementById('reg-error').textContent = '';
    isProcessing = false;
}

function showGameSection() {
    document.getElementById('auth-section').style.display = 'none';
    document.getElementById('game-section').style.display = 'block';
    document.getElementById('current-user').textContent = currentUser.username + ' (' + currentUser.rating + ')';
    initBoard();
}

function showAuthSection() {
    document.getElementById('auth-section').style.display = 'block';
    document.getElementById('game-section').style.display = 'none';
    showLogin();
}

// === Игра ===

function initBoard() {
    board = Chessboard('board', {
        position: 'start',
        draggable: true,
        dropOffBoard: 'snapback',
        pieceTheme: 'https://chessboardjs.com/img/chesspieces/wikipedia/{piece}.png',
        onDrop: onDrop
    });

    createGame(currentUser.userId, 2);
}

async function createGame(whitePlayerId, blackPlayerId) {
    try {
        const response = await fetch('/api/games/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ whitePlayerId, blackPlayerId })
        });

        const data = await response.json();
        currentGameId = data.gameId;
        console.log('Игра создана:', data);
    } catch (error) {
        console.error('Ошибка создания игры:', error);
    }
}

async function onDrop(source, target) {
    if (source === target) return 'snapback';

    try {
        const response = await fetch(`/api/games/${currentGameId}/move`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                from: source,
                to: target,
                color: board.orientation() === 'white' ? 'w' : 'b'
            })
        });

        const data = await response.json();

        if (!data.success) {
            return 'snapback';
        }

        updateTurn();

        if (data.isGameOver) {
            alert('Игра окончена!');
        }
    } catch (error) {
        console.error('Ошибка хода:', error);
        return 'snapback';
    }
}

function updateTurn() {
    var turnSpan = document.getElementById('turn');
    turnSpan.textContent = turnSpan.textContent === 'Белые' ? 'Чёрные' : 'Белые';
}

function resetBoard() {
    board.start();
    document.getElementById('turn').textContent = 'Белые';
    createGame(currentUser.userId, 2);
}

// === Инициализация ===

$(document).ready(function() {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
        currentUser = JSON.parse(savedUser);
        showGameSection();
    } else {
        showAuthSection();
    }
});