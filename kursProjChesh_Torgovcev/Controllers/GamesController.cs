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
        var game = new Game
        {
            WhitePlayerId = request.WhitePlayerId,
            BlackPlayerId = request.BlackPlayerId,
            Status = "Pending",
            CurrentFEN = _chessService.GetCurrentFen()
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

        // Уведомляем второго игрока через SignalR
        await _hubContext.Clients.Group($"game_{id}").SendAsync("MoveMade", new
        {
            from = request.From,
            to = request.To,
            promotion = request.Promotion,
            newFen = result.newFen,
            nextTurn,
            isGameOver,
            sentByUserId = request.UserId  // чтобы получатель знал чей ход
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
        var game = _dbContext.Games.Find(id);
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
            game.BlackPlayerId
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
}

// DTO для запросов
public class CreateGameRequest
{
    public int WhitePlayerId { get; set; }
    public int BlackPlayerId { get; set; }
}

public class MoveRequest
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Color { get; set; } = "w";
    public string Promotion { get; set; } = "q";
    public int UserId { get; set; }  // кто делает ход
}