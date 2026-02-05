using Microsoft.AspNetCore.SignalR;
using kursProjChesh_Torgovcev.services;
using ChessPlatform.Data;
using ChessPlatform.Models;

namespace kursProjChesh_Torgovcev.Hubs;

public class GameHub : Hub
{
    private readonly ChessService _chessService;
    private readonly ChessDbContext _dbContext;

    public GameHub(ChessService chessService, ChessDbContext dbContext)
    {
        _chessService = chessService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Присоединиться к игре (комната)
    /// </summary>
    public async Task JoinGame(int gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"game_{gameId}");
        await Clients.Group($"game_{gameId}").SendAsync("PlayerJoined", Context.ConnectionId);
    }

    /// <summary>
    /// Покинуть игру
    /// </summary>
    public async Task LeaveGame(int gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game_{gameId}");
        await Clients.Group($"game_{gameId}").SendAsync("PlayerLeft", Context.ConnectionId);
    }

    /// <summary>
    /// Сделать ход (real-time)
    /// </summary>
    public async Task MakeMove(int gameId, string from, string to, string playerColor)
    {
        // Проверяем ход
        var result = _chessService.MakeMove(from, to);
        
        if (!result.success)
        {
            await Clients.Caller.SendAsync("InvalidMove", "Недопустимый ход");
            return;
        }

        // Обновляем игру в БД
        var game = await _dbContext.Games.FindAsync(gameId);
        if (game != null)
        {
            game.CurrentFEN = result.newFen;
            game.LastMoveAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        // Отправляем ход всем в комнате (включая отправителя)
        await Clients.Group($"game_{gameId}").SendAsync("MoveMade", new
        {
            from,
            to,
            newFen = result.newFen,
            pgnMove = result.pgnMove,
            isGameOver = _chessService.IsGameOver(),
            nextTurn = playerColor == "w" ? "b" : "w"
        });
    }

    /// <summary>
    /// Предложить ничью
    /// </summary>
    public async Task OfferDraw(int gameId)
    {
        await Clients.Group($"game_{gameId}").SendAsync("DrawOffered", Context.ConnectionId);
    }

    /// <summary>
    /// Сдаться
    /// </summary>
    public async Task Resign(int gameId, string playerColor)
    {
        await Clients.Group($"game_{gameId}").SendAsync("GameEnded", new
        {
            reason = "resign",
            winner = playerColor == "w" ? "b" : "w"
        });
    }

    /// <summary>
    /// Отправить сообщение в чат игры
    /// </summary>
    public async Task SendMessage(int gameId, string message)
    {
        await Clients.Group($"game_{gameId}").SendAsync("ReceiveMessage", new
        {
            sender = Context.ConnectionId,
            message,
            timestamp = DateTime.Now
        });
    }
}