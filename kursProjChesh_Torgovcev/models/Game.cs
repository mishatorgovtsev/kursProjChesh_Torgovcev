using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Статусы игры
/// </summary>
public enum GameStatusEnum
{
    Pending,    // Ожидание начала
    Active,     // Идёт сейчас
    Completed,  // Завершена
    Aborted     // Прервана
}

/// <summary>
/// Результаты игры
/// </summary>
public enum GameResultEnum
{
    WhiteWin,   // Победа белых
    BlackWin,   // Победа чёрных
    Draw        // Ничья
}

/// <summary>
/// Модель шахматной партии
/// Хранит состояние доски (FEN), таймеры, историю ходов
/// </summary>
[Table("game")]
public class Game
{
    [Key]
    [Column("ID_game")]
    public int Id { get; set; }

    // Игрок за белых
    [ForeignKey("WhitePlayer")]
    [Column("ID_white_player")]
    public int WhitePlayerId { get; set; }
    public User WhitePlayer { get; set; } = null!;

    // Игрок за чёрных
    [ForeignKey("BlackPlayer")]
    [Column("ID_black_player")]
    public int BlackPlayerId { get; set; }
    public User BlackPlayer { get; set; } = null!;

    // Победитель (NULL если ничья или игра не завершена)
    [ForeignKey("Winner")]
    [Column("ID_winner")]
    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    [Column("result")]
    [MaxLength(20)]
    public string? Result { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    // Контроль времени
    [Column("time_control_minutes")]
    public int TimeControlMinutes { get; set; } = 90;

    [Column("time_control_increment")]
    public int TimeControlIncrement { get; set; } = 0;

    // Текущая позиция на доске в нотации FEN
    [Column("current_fen")]
    [MaxLength(100)]
    public string CurrentFEN { get; set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Полная запись партии в формате PGN
    [Column("pgn")]
    public string? PGN { get; set; }

    // Оставшееся время в секундах
    [Column("white_time_remaining")]
    public int WhiteTimeRemaining { get; set; } = 5400;

    [Column("black_time_remaining")]
    public int BlackTimeRemaining { get; set; } = 5400;

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("last_move_at")]
    public DateTime? LastMoveAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Связь с турниром
    [ForeignKey("Tournament")]
    [Column("ID_tournament")]
    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    [Column("tournament_round")]
    public int? TournamentRound { get; set; }

    // Навигационные свойства
    public ICollection<GameMove> Moves { get; set; } = new List<GameMove>();
    public ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();
}