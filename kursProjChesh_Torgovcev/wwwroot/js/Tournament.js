// ═══════════════════════════════════════════════════
//  Chess Platform — tournament.js
//  Логика страницы турниров
// ═══════════════════════════════════════════════════

var currentTournamentId = null;
var currentTournament   = null;
var currentMatches      = [];
var currentFilter       = 'all';
var isCreating          = false;

// ── Инициализация ─────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var user = loadCurrentUser();
    if (!user) { window.location.href = '/'; return; }
    initHeader();

    // Дефолтная дата — завтра 12:00
    var tomorrow = new Date(Date.now() + 86400000);
    tomorrow.setHours(12, 0, 0, 0);
    document.getElementById('c-date').value = tomorrow.toISOString().slice(0, 16);

    // Если в URL есть ?id= — открываем турнир сразу
    var params = new URLSearchParams(window.location.search);
    var urlId  = params.get('id');
    if (urlId) openTournament(parseInt(urlId));
    else loadTournamentList();

    // Закрыть модал кликом вне
    document.getElementById('match-modal').addEventListener('click', function (e) {
        if (e.target === document.getElementById('match-modal')) closeModal();
    });
});

// ── Вкладки ───────────────────────────────────────
function switchTab(tab) {
    document.querySelectorAll('.tab-btn').forEach(function (b, i) {
        b.classList.toggle('active', (i === 0) === (tab === 'list'));
    });
    document.getElementById('tab-list').classList.toggle('active', tab === 'list');
    document.getElementById('tab-create').classList.toggle('active', tab === 'create');
}

// ── Фильтр списка ─────────────────────────────────
function setFilter(status, btn) {
    currentFilter = status;
    document.querySelectorAll('.filter-btn').forEach(function (b) { b.classList.remove('active'); });
    btn.classList.add('active');
    loadTournamentList();
}

// ── Список турниров ───────────────────────────────
async function loadTournamentList() {
    var loader = document.getElementById('list-loader');
    var grid   = document.getElementById('tournaments-grid');
    loader.classList.add('visible');
    grid.innerHTML = '';

    try {
        var url = currentFilter === 'all'
            ? '/api/Tournaments/all'
            : '/api/Tournaments/list?status=' + currentFilter;

        var res  = await fetch(url);
        var list = await res.json();
        loader.classList.remove('visible');

        if (!list || !list.length) {
            grid.innerHTML = '<div class="empty-state"><div class="icon">🏆</div><p>Турниров пока нет.<br>Создайте первый!</p></div>';
            return;
        }

        grid.innerHTML = list.map(function (t) {
            var pct    = Math.round((t.currentParticipants || 0) / t.maxParticipants * 100);
            var badge  = getBadge(t.status);
            var starts = t.startsAt
                ? new Date(t.startsAt).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
                : '-';
            return '<div class="t-card" onclick="openTournament(' + t.id + ')">' +
                '<div class="t-card-title">' + escHtml(t.title) + '</div>' +
                '<div class="t-card-desc">' + escHtml(t.description || '') + '</div>' +
                '<div class="t-card-meta">' +
                '<span>⏱ ' + t.timeControlMinutes + ' мин</span>' +
                '<span>📅 ' + starts + '</span>' +
                '</div>' +
                '<div class="t-card-footer">' +
                '<div class="t-participants-bar">' +
                '<div class="t-participants-label">' + (t.currentParticipants || 0) + ' / ' + t.maxParticipants + ' участников</div>' +
                '<div class="t-participants-track"><div class="t-participants-fill" style="width:' + pct + '%"></div></div>' +
                '</div>' +
                badge +
                '</div>' +
                '</div>';
        }).join('');

    } catch (e) {
        loader.classList.remove('visible');
        grid.innerHTML = '<div class="empty-state"><div class="icon">⚠️</div><p>Ошибка загрузки</p></div>';
    }
}

function getBadge(status) {
    var map = {
        Registration: ['t-badge-reg',  'Регистрация'],
        InProgress:   ['t-badge-live', 'Идёт'],
        Completed:    ['t-badge-done', 'Завершён']
    };
    var b = map[status] || ['t-badge-reg', status];
    return '<span class="t-badge ' + b[0] + '">' + b[1] + '</span>';
}

// ── Открыть турнир ────────────────────────────────
async function openTournament(id) {
    currentTournamentId = id;
    document.getElementById('view-list').style.display   = 'none';
    document.getElementById('view-detail').style.display = 'block';
    history.pushState(null, '', '/tournament.html?id=' + id);

    try {
        var res = await fetch('/api/Tournaments/' + id);
        if (!res.ok) throw new Error('Турнир не найден');
        currentTournament = await res.json();
        renderDetail(currentTournament);
        if (currentTournament.status !== 'Registration') loadBracket(id);
    } catch (e) {
        toast(e.message, 'error');
        showList();
    }
}

function showList() {
    document.getElementById('view-list').style.display   = 'block';
    document.getElementById('view-detail').style.display = 'none';
    currentTournamentId = null;
    history.pushState(null, '', '/tournament.html');
    loadTournamentList();
}

// ── Рендер деталей турнира ────────────────────────
function renderDetail(t) {
    var user = loadCurrentUser();

    document.getElementById('d-title').textContent = t.title;
    document.getElementById('d-desc').textContent  = t.description || '';

    var badgeMap = { Registration: 't-badge-reg', InProgress: 't-badge-live', Completed: 't-badge-done' };
    var labelMap = { Registration: 'Регистрация', InProgress: 'Идёт', Completed: 'Завершён' };
    document.getElementById('d-badge').className   = 't-badge ' + (badgeMap[t.status] || '');
    document.getElementById('d-badge').textContent = labelMap[t.status] || t.status;

    document.getElementById('d-participants').textContent = t.currentParticipants || 0;
    document.getElementById('d-max').textContent          = t.maxParticipants;
    document.getElementById('d-rounds').textContent       = t.totalRounds || Math.round(Math.log2(t.maxParticipants));
    document.getElementById('d-time').textContent         = t.timeControlMinutes;

    var starts = t.startsAt
        ? new Date(t.startsAt).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
        : '-';
    document.getElementById('d-starts').textContent = starts;

    // Участники
    var participants = t.participants || [];
    var grid = document.getElementById('participants-grid');
    if (!participants.length) {
        grid.innerHTML = '<div style="color:var(--text-muted);font-size:14px;grid-column:1/-1">Участников пока нет</div>';
    } else {
        grid.innerHTML = participants.map(function (p) {
            return '<div class="participant-card">' +
                '<div class="participant-seed">#' + (p.seedNumber || '?') + '</div>' +
                '<div class="participant-name" onclick="goToProfile(' + p.userId + ')">' + escHtml(p.username) + '</div>' +
                '<div class="participant-rating">' + p.rating + '</div>' +
                '</div>';
        }).join('');
    }

    // Кнопки управления
    var isParticipant = participants.some(function (p) { return parseInt(p.userId) === parseInt(user.userId); });
    var isCreator     = parseInt(t.creatorId) === parseInt(user.userId);

    document.getElementById('btn-join').style.display  = (t.status === 'Registration' && !isParticipant) ? 'inline-block' : 'none';
    document.getElementById('btn-leave').style.display = (t.status === 'Registration' && isParticipant)  ? 'inline-block' : 'none';
    document.getElementById('btn-start').style.display = (t.status === 'Registration' && isCreator && participants.length >= 2) ? 'inline-block' : 'none';

    document.getElementById('bracket-section').style.display = t.status !== 'Registration' ? 'block' : 'none';

    // Победитель
    document.getElementById('winner-banner').classList.toggle('visible', t.status === 'Completed');
    if (t.status === 'Completed' && t.winner) {
        document.getElementById('winner-name').textContent = t.winner.username || '-';
    }
}

// ── Турнирная сетка ───────────────────────────────
async function loadBracket(id) {
    var loader    = document.getElementById('bracket-loader');
    var container = document.getElementById('bracket-container');
    loader.classList.add('visible');
    container.innerHTML = '';

    try {
        var res = await fetch('/api/Tournaments/' + id + '/bracket');
        currentMatches = await res.json();
        loader.classList.remove('visible');
        renderBracket(currentMatches);
    } catch (e) {
        loader.classList.remove('visible');
    }
}

function renderBracket(matches) {
    var container = document.getElementById('bracket-container');

    if (!matches || !matches.length) {
        container.innerHTML = '<div class="empty-state"><div class="icon">🏁</div><p>Сетка не сгенерирована</p></div>';
        return;
    }

    var rounds = {};
    matches.forEach(function (m) {
        if (!rounds[m.roundNumber]) rounds[m.roundNumber] = [];
        rounds[m.roundNumber].push(m);
    });

    var roundNumbers = Object.keys(rounds).map(Number).sort(function (a, b) { return a - b; });
    var totalRounds  = roundNumbers.length;
    var roundNames   = {};

    roundNumbers.forEach(function (r, i) {
        var rem = totalRounds - i;
        if (rem === 1)      roundNames[r] = 'Финал';
        else if (rem === 2) roundNames[r] = 'Полуфинал';
        else if (rem === 3) roundNames[r] = 'Четвертьфинал';
        else                roundNames[r] = 'Раунд ' + r;
    });

    var CARD_H = 110;
    container.innerHTML = '';

    roundNumbers.forEach(function (roundNum, roundIdx) {
        var roundMatches = rounds[roundNum].sort(function (a, b) { return a.matchNumber - b.matchNumber; });
        var matchesCount = roundMatches.length;
        var totalPrev    = roundIdx > 0 ? rounds[roundNumbers[roundIdx - 1]].length : matchesCount;
        var gap          = roundIdx === 0 ? 16 : ((totalPrev / matchesCount) * (CARD_H + 16) - CARD_H);

        var roundCol = document.createElement('div');
        roundCol.className = 'round-col';

        var label = document.createElement('div');
        label.className   = 'round-label';
        label.textContent = roundNames[roundNum];
        roundCol.appendChild(label);

        var matchesEl = document.createElement('div');
        matchesEl.className       = 'round-matches';
        matchesEl.style.gap       = Math.max(gap, 16) + 'px';
        matchesEl.style.display   = 'flex';
        matchesEl.style.flexDirection = 'column';

        roundMatches.forEach(function (match) {
            var card = document.createElement('div');
            card.className = 'match-card' + (match.isFinal ? ' is-final' : '');

            card.appendChild(renderPlayerEl(match.player1, match.winner));
            card.appendChild(renderPlayerEl(match.player2, match.winner));

            var statusBar = document.createElement('div');
            var sMap = { Pending: 'Ожидание', Ready: 'Готов', InProgress: 'Идёт', Completed: 'Завершён' };
            statusBar.className   = 'match-status ' + match.status;
            statusBar.textContent = sMap[match.status] || match.status;
            card.appendChild(statusBar);

            if (match.status !== 'Completed' && match.player1 && match.player2) {
                card.style.cursor = 'pointer';

                if (match.status === 'Ready') {
                    var startBtn = document.createElement('button');
                    startBtn.className   = 'btn btn-primary';
                    startBtn.textContent = '▶ Начать партию';
                    startBtn.style.cssText = 'width:100%;border-radius:0;font-size:12px;padding:7px;';
                    startBtn.addEventListener('click', (function (m) {
                        return function (e) { e.stopPropagation(); startMatch(m); };
                    })(match));
                    card.appendChild(startBtn);
                } else if (match.status === 'InProgress' && match.gameId) {
                    var goBtn = document.createElement('button');
                    goBtn.className   = 'btn btn-ghost';
                    goBtn.textContent = '→ Перейти в игру';
                    goBtn.style.cssText = 'width:100%;border-radius:0;font-size:12px;padding:7px;';
                    goBtn.addEventListener('click', (function (m) {
                        return function (e) { e.stopPropagation(); goToMatch(m); };
                    })(match));
                    card.appendChild(goBtn);
                } else {
                    card.title = 'Нажмите чтобы указать победителя';
                    card.addEventListener('click', (function (m) {
                        return function () { openModal(m); };
                    })(match));
                }
            }

            matchesEl.appendChild(card);
        });

        roundCol.appendChild(matchesEl);
        container.appendChild(roundCol);

        if (roundIdx < roundNumbers.length - 1) {
            container.appendChild(buildConnector(matchesCount, CARD_H, gap));
        }
    });
}

function renderPlayerEl(player, winner) {
    var div      = document.createElement('div');
    var isWinner = winner && player && winner.id === player.id;
    div.className = 'match-player' + (isWinner ? ' winner' : '');

    var name = document.createElement('span');
    name.className   = 'player-name' + (!player ? ' empty' : '');
    name.textContent = player ? player.username : 'TBD';

    var crown = document.createElement('span');
    crown.className   = 'player-crown';
    crown.textContent = isWinner ? '✓' : '';

    div.appendChild(name);
    div.appendChild(crown);
    return div;
}

function buildConnector(matchCount, cardH, gap) {
    var connEl = document.createElement('div');
    connEl.className = 'connector';

    var W    = 40;
    var g    = Math.max(gap, 16);
    var svgH = matchCount * (cardH + g) + 16;
    if (matchCount === 1) svgH = cardH + 32;

    var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('width', W);
    svg.setAttribute('height', svgH);
    svg.style.marginTop = '38px';

    var pairs = Math.ceil(matchCount / 2);
    var pairH = (cardH + g) * 2;

    for (var i = 0; i < pairs; i++) {
        var topY = i * pairH + cardH / 2 + 8;
        var botY = topY + (cardH + g);
        var midY = (topY + botY) / 2;

        var path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', 'M0,' + topY + ' H' + (W / 2) + ' V' + botY + ' M' + (W / 2) + ',' + midY + ' H' + W);
        path.setAttribute('fill', 'none');
        path.setAttribute('stroke', '#2e2e3e');
        path.setAttribute('stroke-width', '1.5');
        svg.appendChild(path);
    }

    if (matchCount === 1) {
        var line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        var y = cardH / 2 + 8;
        line.setAttribute('x1', 0); line.setAttribute('y1', y);
        line.setAttribute('x2', W); line.setAttribute('y2', y);
        line.setAttribute('stroke', '#2e2e3e');
        line.setAttribute('stroke-width', '1.5');
        svg.appendChild(line);
    }

    connEl.appendChild(svg);
    return connEl;
}

// ── Создать турнир ────────────────────────────────
async function createTournament() {
    if (isCreating) return;
    var errEl = document.getElementById('create-error');
    errEl.textContent = '';

    var user    = loadCurrentUser();
    var title   = document.getElementById('c-title').value.trim();
    var desc    = document.getElementById('c-desc').value.trim();
    var max     = parseInt(document.getElementById('c-max').value);
    var time    = parseInt(document.getElementById('c-time').value);
    var dateVal = document.getElementById('c-date').value;

    if (!title)   { errEl.textContent = 'Введите название турнира'; return; }
    if (!dateVal) { errEl.textContent = 'Выберите дату начала'; return; }

    isCreating = true;
    var btn = document.getElementById('create-btn');
    btn.disabled    = true;
    btn.textContent = 'Создание...';

    try {
        var res = await fetch('/api/Tournaments/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                title:              title,
                description:        desc,
                creatorId:          user.userId,
                maxParticipants:    max,
                timeControlMinutes: time,
                startsAt:           new Date(dateVal).toISOString()
            })
        });
        var data = await res.json();

        if (res.ok) {
            toast('Турнир создан!', 'success');
            document.getElementById('c-title').value = '';
            document.getElementById('c-desc').value  = '';
            openTournament(data.tournamentId);
        } else {
            errEl.textContent = data.error || 'Ошибка создания';
        }
    } catch (e) {
        errEl.textContent = 'Ошибка сети';
    } finally {
        isCreating      = false;
        btn.disabled    = false;
        btn.textContent = '♟ Создать турнир';
    }
}

// ── Записаться / Отписаться ───────────────────────
async function joinTournament() {
    var user = loadCurrentUser();
    try {
        var res = await fetch('/api/Tournaments/' + currentTournamentId + '/join', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: user.userId })
        });
        if (res.ok) {
            toast('Вы записаны в турнир!', 'success');
            openTournament(currentTournamentId);
        } else {
            var d = await res.text();
            toast(d || 'Ошибка', 'error');
        }
    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

async function leaveTournament() {
    var user = loadCurrentUser();
    try {
        var res = await fetch('/api/Tournaments/' + currentTournamentId + '/leave', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: user.userId })
        });
        if (res.ok) {
            toast('Вы отписаны от турнира', 'success');
            openTournament(currentTournamentId);
        } else {
            toast('Ошибка отписки', 'error');
        }
    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Запустить турнир ──────────────────────────────
async function startTournament() {
    try {
        var res  = await fetch('/api/Tournaments/' + currentTournamentId + '/start', { method: 'POST' });
        var data = await res.json();
        if (res.ok) {
            toast('Турнир запущен! Сетка сгенерирована.', 'success');
            openTournament(currentTournamentId);
        } else {
            toast(data || 'Ошибка запуска', 'error');
        }
    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Модал завершения матча ────────────────────────
function openModal(match) {
    var playersEl = document.getElementById('modal-players');
    playersEl.innerHTML = '';

    [match.player1, match.player2].forEach(function (p) {
        if (!p) return;
        var btn = document.createElement('button');
        btn.className   = 'modal-player-btn';
        btn.textContent = '🏆  ' + p.username;
        btn.onclick     = function () { completeMatch(match.id, p.id); };
        playersEl.appendChild(btn);
    });

    document.getElementById('match-modal').classList.add('open');
}

function closeModal() {
    document.getElementById('match-modal').classList.remove('open');
}

async function completeMatch(matchId, winnerId) {
    closeModal();
    try {
        var res = await fetch('/api/Tournaments/' + currentTournamentId + '/matches/' + matchId + '/complete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ winnerId: winnerId })
        });
        if (res.ok) {
            toast('Победитель записан!', 'success');
            openTournament(currentTournamentId);
        } else {
            var e = await res.text();
            toast(e || 'Ошибка', 'error');
        }
    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Старт турнирного матча ────────────────────────
async function startMatch(match) {
    try {
        var res  = await fetch('/api/Tournaments/' + currentTournamentId + '/matches/' + match.id + '/start', {
            method: 'POST'
        });
        var data = await res.json();

        if (!res.ok) {
            toast(data.error || 'Ошибка старта матча', 'error');
            return;
        }

        toast('Игра создана! Переходим...', 'success');

        // Переходим в игру через goToGame из common.js
        goToGame(
            data.gameId,
            data.whiteId,
            data.blackId,
            data.whiteName,
            data.blackName,
            data.timeMin
        );

    } catch (e) {
        toast('Ошибка сети', 'error');
    }
}

// ── Перейти в уже созданную игру матча ───────────
function goToMatch(match) {
    if (!match.gameId) {
        toast('Игра не создана', 'error');
        return;
    }

    var whiteName = match.player1 ? match.player1.username : 'Белые';
    var blackName = match.player2 ? match.player2.username : 'Чёрные';
    var whiteId   = match.player1 ? match.player1.id : 0;
    var blackId   = match.player2 ? match.player2.id : 0;
    var timeMin   = currentTournament ? currentTournament.timeControlMinutes : 10;

    goToGame(match.gameId, whiteId, blackId, whiteName, blackName, timeMin);
}