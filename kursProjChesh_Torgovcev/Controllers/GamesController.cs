using Microsoft.AspNetCore.Mvc;
using kursProjChesh_Torgovcev.services;
using ChessPlatform.Data;
using ChessPlatform.Models;

namespace kursProjChesh_Torgovcev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly ChessService _chessService;
    private readonly ChessDbContext _dbContext;

    public GamesController(ChessService chessService, ChessDbContext dbContext)
    {
        _chessService = chessService;
        _dbContext = dbContext;
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
    public IActionResult MakeMove(int id, [FromBody] MoveRequest request)
    {
        var game = _dbContext.Games.Find(id);
        if (game == null)
            return NotFound("Игра не найдена");

        // Проверяем ход через ChessService
        var result = _chessService.MakeMove(request.From, request.To);
        
        if (!result.success)
            return BadRequest("Недопустимый ход");

        // Обновляем игру в БД
        game.CurrentFEN = result.newFen;
        game.LastMoveAt = DateTime.Now;
        
        // Добавляем ход в историю
        var move = new GameMove
        {
            GameId = id,
            MoveNumber = game.Moves.Count + 1,
            PlayerColor = request.Color,
            MoveNotation = result.pgnMove,
            FENAfterMove = result.newFen
        };
        
        _dbContext.GameMoves.Add(move);
        _dbContext.SaveChanges();

        return Ok(new 
        { 
            success = true, 
            newFen = result.newFen, 
            isGameOver = _chessService.IsGameOver() 
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
            return NotFound();

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
}

// DTO для запросов
public class CreateGameRequest
{
    public int WhitePlayerId { get; set; }
    public int BlackPlayerId { get; set; }
}

public class MoveRequest
{
    public string From { get; set; } = string.Empty;  // e2
    public string To { get; set; } = string.Empty;    // e4
    public string Color { get; set; } = "w";          // w или b
}