console.log('=== app.js started ===');

var board = null;

if (typeof jQuery !== 'undefined') {
    console.log('jQuery loaded successfully');
} else {
    console.error('jQuery NOT loaded!');
}

if (typeof Chessboard !== 'undefined') {
    console.log('Chessboard loaded successfully');
} else {
    console.error('Chessboard NOT loaded!');
}

$(document).ready(function() {
    console.log('=== document ready ===');

    try {
        board = Chessboard('board', {
            position: 'start',
            draggable: true,
            dropOffBoard: 'snapback',
            pieceTheme: 'https://chessboardjs.com/img/chesspieces/wikipedia/{piece}.png'  // ← ДОБАВИЛИ
        });
        console.log('Board created successfully:', board);
    } catch(e) {
        console.error('Error creating board:', e);
    }
});

function updateTurn() {
    var turnSpan = document.getElementById('turn');
    if (turnSpan) {
        turnSpan.textContent = turnSpan.textContent === 'Белые' ? 'Чёрные' : 'Белые';
    }
}

function resetBoard() {
    console.log('resetBoard called, board:', board);
    if (board) {
        board.start();
        var turnSpan = document.getElementById('turn');
        if (turnSpan) turnSpan.textContent = 'Белые';
    } else {
        console.error('Board not initialized!');
    }
}

console.log('=== app.js finished ===');