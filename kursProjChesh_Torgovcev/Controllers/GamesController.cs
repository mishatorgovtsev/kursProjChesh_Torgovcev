using Microsoft.AspNetCore.Mvc;
using kursProjChesh_Torgovcev.services;
using ChessPlatform.Data;
using ChessPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using kursProjChesh_Torgovcev.Hubs;

namespace kursProjChesh_Torgovcev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly ChessService _chessService;
    private readonly ChessDbContext _dbContext;
    private readonly IHubContext<GameHub> _hubContext;

    public GamesController(ChessService chessService, ChessDbContext dbContext, IHubContext<GameHub> hubContext)
    {
        _chessService = chessService;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Создать новую игру
    /// </summary>
    [HttpPost("create")]
    public IActionResult CreateGame([FromBody] CreateGameRequest request)
    {
        var timeMin = request.TimeControlMinutes > 0 ? request.TimeControlMinutes : 10;
        var game = new Game
        {
            WhitePlayerId      = request.WhitePlayerId,
            BlackPlayerId      = request.BlackPlayerId,
            Status             = "Pending",
            CurrentFEN         = _chessService.GetCurrentFen(),
            TimeControlMinutes = timeMin,
            WhiteTimeRemaining = timeMin * 60,
            BlackTimeRemaining = timeMin * 60
        };

        _dbContext.Games.Add(game);
        _dbContext.SaveChanges();

        return Ok(new { gameId = game.Id, message = "Игра создана" });
    }

    /// <summary>
    /// Сделать ход
    /// </summary>
    [HttpPost("{id}/move")]
    public async Task<IActionResult> MakeMove(int id, [FromBody] MoveRequest request)
    {
        var game = _dbContext.Games.Include(g => g.Moves).FirstOrDefault(g => g.Id == id);
        if (game == null)
            return NotFound(new { success = false, error = "Игра не найдена" });

        // Восстанавливаем состояние доски из БД
        var chessService = new ChessService();
        chessService.LoadPosition(game.CurrentFEN);

        // Проверяем ход
        var result = chessService.MakeMove(request.From, request.To, request.Promotion);

        if (!result.success)
            return BadRequest(new { success = false, error = "Недопустимый ход" });

        // Обновляем игру в БД
        game.CurrentFEN = result.newFen;

        // Вычитаем прошедшее время у того кто ходил
        if (game.LastMoveAt.HasValue)
        {
            var elapsed = (int)(DateTime.Now - game.LastMoveAt.Value).TotalSeconds;
            if (request.Color == "white")
                game.WhiteTimeRemaining = Math.Max(0, game.WhiteTimeRemaining - elapsed);
            else
                game.BlackTimeRemaining = Math.Max(0, game.BlackTimeRemaining - elapsed);
        }
        else
        {
            // Первый ход — инициализируем время из TimeControlMinutes
            game.WhiteTimeRemaining = game.TimeControlMinutes * 60;
            game.BlackTimeRemaining = game.TimeControlMinutes * 60;
        }
        game.LastMoveAt = DateTime.Now;

        // Добавляем ход в историю
        var move = new GameMove
        {
            GameId = id,
            MoveNumber = game.Moves.Count + 1,
            PlayerColor = request.Color == "white" ? "w" : "b",
            MoveNotation = result.pgnMove,
            FENAfterMove = result.newFen
        };

        _dbContext.GameMoves.Add(move);
        _dbContext.SaveChanges();

        var isGameOver = chessService.IsGameOver();
        var nextTurn = request.Color == "white" ? "b" : "w";

        // Уведомляем ВСЕХ игроков через SignalR (включая отправителя — для синхронизации таймера)
        await _hubContext.Clients.Group($"game_{id}").SendAsync("MoveMade", new
        {
            from = request.From,
            to = request.To,
            promotion = request.Promotion,
            newFen = result.newFen,
            nextTurn,
            isGameOver,
            sentByUserId = request.UserId,
            whiteTimeSec = game.WhiteTimeRemaining,
            blackTimeSec = game.BlackTimeRemaining
        });

        return Ok(new
        {
            success = true,
            newFen = result.newFen,
            isGameOver
        });
    }

    /// <summary>
    /// Получить текущее состояние игры
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetGame(int id)
    {
        var game = _dbContext.Games
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .Include(g => g.Moves)
            .FirstOrDefault(g => g.Id == id);

        if (game == null)
            return NotFound(new { success = false, error = "Игра не найдена" });

        return Ok(new
        {
            game.Id,
            game.Status,
            game.CurrentFEN,
            game.WhiteTimeRemaining,
            game.BlackTimeRemaining,
            game.WhitePlayerId,
            game.BlackPlayerId,
            game.TimeControlMinutes,
            WhiteName = game.WhitePlayer.Username,
            BlackName = game.BlackPlayer.Username,
            Moves = game.Moves
                .OrderBy(m => m.MoveNumber)
                .Select(m => new { m.MoveNumber, m.PlayerColor, m.MoveNotation, m.FENAfterMove })
                .ToList()
        });
    }

    /// <summary>
    /// Отменить последний ход
    /// </summary>
    [HttpPost("{id}/undo")]
    public async Task<IActionResult> UndoMove(int id)
    {
        var game = _dbContext.Games
            .Include(g => g.Moves)
            .FirstOrDefault(g => g.Id == id);

        if (game == null)
            return NotFound(new { success = false, error = "Игра не найдена" });

        if (game.Moves.Count == 0)
            return BadRequest(new { success = false, error = "Нет ходов для отмены" });

        // Удаляем последний ход
        var lastMove = game.Moves.OrderByDescending(m => m.MoveNumber).First();
        _dbContext.GameMoves.Remove(lastMove);

        // Восстанавливаем FEN из предпоследнего хода
        var prevMove = game.Moves
            .OrderByDescending(m => m.MoveNumber)
            .Skip(1)
            .FirstOrDefault();

        game.CurrentFEN = prevMove?.FENAfterMove ?? "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        _dbContext.SaveChanges();

        // Уведомляем второго игрока об отмене хода
        await _hubContext.Clients.Group($"game_{id}").SendAsync("MoveUndone", new
        {
            fen = game.CurrentFEN
        });

        return Ok(new { success = true, fen = game.CurrentFEN });
    }
    /// <summary>
     /// Получить список активных игр
     /// </summary>
     [HttpGet("active")]
     public IActionResult GetActiveGames([FromQuery] int userId = 0)
     {
         var games = _dbContext.Games
             .Include(g => g.WhitePlayer)
             .Include(g => g.BlackPlayer)
             .Where(g => g.Status == "Pending" || g.Status == "Active")
             .OrderByDescending(g => g.CreatedAt)
             .Take(20)
             .Select(g => new
             {
                 id = g.Id,
                 whitePlayer = new { id = g.WhitePlayerId, username = g.WhitePlayer.Username },
                 blackPlayer = new { id = g.BlackPlayerId, username = g.BlackPlayer.Username },
                 status = g.Status,
                 timeControlMinutes = g.TimeControlMinutes,
                 createdAt = g.CreatedAt,
                 isMyGame = userId > 0 && (g.WhitePlayerId == userId || g.BlackPlayerId == userId)
             })
             .ToList();
     
         return Ok(games);
     }
    
    /// <summary>
/// Завершить игру и обновить рейтинг
/// </summary>
[HttpPost("{id}/finish")]
public async Task<IActionResult> FinishGame(int id, [FromBody] FinishGameRequest request)
{
    var game = _dbContext.Games
        .Include(g => g.WhitePlayer)
        .Include(g => g.BlackPlayer)
        .FirstOrDefault(g => g.Id == id);

    if (game == null)
        return NotFound(new { success = false, error = "Игра не найдена" });

    if (game.Status == "Completed")
        return BadRequest(new { success = false, error = "Игра уже завершена" });

    game.Status = "Completed";
    game.EndedAt = DateTime.Now;
    game.Result = request.Result;
    game.WinnerId = request.WinnerId;

    // Считаем ELO
    var white = game.WhitePlayer;
    var black = game.BlackPlayer;

    double score = request.Result == "white" ? 1.0 : request.Result == "black" ? 0.0 : 0.5;

    int newWhiteRating = CalculateElo(white.Rating, black.Rating, score);
    int newBlackRating = CalculateElo(black.Rating, white.Rating, 1.0 - score);

    // Сохраняем историю рейтинга
    _dbContext.RatingHistories.AddRange(
        new RatingHistory { UserId = white.Id, GameId = id, OldRating = white.Rating, NewRating = newWhiteRating, RatingChange = newWhiteRating - white.Rating, RecordedAt = DateTime.Now },
        new RatingHistory { UserId = black.Id, GameId = id, OldRating = black.Rating, NewRating = newBlackRating, RatingChange = newBlackRating - black.Rating, RecordedAt = DateTime.Now }
    );

    white.Rating = newWhiteRating;
    white.GamesPlayed++;
    if (request.Result == "white") white.Wins++;
    else if (request.Result == "black") white.Losses++;
    else white.Draws++;

    black.Rating = newBlackRating;
    black.GamesPlayed++;
    if (request.Result == "black") black.Wins++;
    else if (request.Result == "white") black.Losses++;
    else black.Draws++;

    _dbContext.SaveChanges();

    // Если игра была частью турнирного матча — завершаем матч автоматически
    var tournamentMatch = _dbContext.TournamentMatches
        .FirstOrDefault(m => m.GameId == id);

    if (tournamentMatch != null && tournamentMatch.Status != "Completed")
    {
        // Определяем победителя матча
        int? matchWinnerId = null;
        if (request.Result == "white") matchWinnerId = game.WhitePlayerId;
        else if (request.Result == "black") matchWinnerId = game.BlackPlayerId;
        // При ничьей побеждает игрок 1 (Player1 = белые)
        else matchWinnerId = game.WhitePlayerId;

        tournamentMatch.WinnerId = matchWinnerId;
        tournamentMatch.Status   = "Completed";

        // Продвигаем победителя в следующий матч
        if (tournamentMatch.NextMatchId != null)
        {
            var nextMatch = _dbContext.TournamentMatches.Find(tournamentMatch.NextMatchId);
            if (nextMatch != null)
            {
                if (nextMatch.Player1Id == null) nextMatch.Player1Id = matchWinnerId;
                else                             nextMatch.Player2Id = matchWinnerId;

                if (nextMatch.Player1Id != null && nextMatch.Player2Id != null)
                    nextMatch.Status = "Ready";
            }
        }
        else if (tournamentMatch.IsFinal)
        {
            var tournament = _dbContext.Tournaments.Find(tournamentMatch.TournamentId);
            if (tournament != null)
            {
                tournament.WinnerId = matchWinnerId;
                tournament.Status   = "Completed";
            }
        }

        _dbContext.SaveChanges();
    }

    // Уведомляем через SignalR
    await _hubContext.Clients.Group($"game_{id}").SendAsync("GameFinished", new
    {
        result = request.Result,
        winnerId = request.WinnerId,
        whiteRating = new { old = white.Rating - (newWhiteRating - white.Rating), newR = newWhiteRating },
        blackRating = new { old = black.Rating - (newBlackRating - black.Rating), newR = newBlackRating }
    });

    return Ok(new { success = true, whiteNewRating = newWhiteRating, blackNewRating = newBlackRating });
}

private int CalculateElo(int playerRating, int opponentRating, double score)
{
    double expected = 1.0 / (1.0 + Math.Pow(10, (opponentRating - playerRating) / 400.0));
    int k = playerRating < 2100 ? 32 : playerRating < 2400 ? 24 : 16;
    return (int)Math.Round(playerRating + k * (score - expected));
}
}



// DTO для запросов
public class CreateGameRequest
{
    public int WhitePlayerId { get; set; }
    public int BlackPlayerId { get; set; }
    public int TimeControlMinutes { get; set; } = 10;
}

public class MoveRequest
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Color { get; set; } = "w";
    public string Promotion { get; set; } = "q";
    public int UserId { get; set; } // кто делает ход
}

public class FinishGameRequest
{
    public string Result { get; set; } = "white"; // "white", "black", "draw"
    public int? WinnerId { get; set; }
}