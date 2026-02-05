using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Статусы матча в турнире
/// </summary>
public enum MatchStatusEnum
{
    Pending,    // Ожидание (игроки неизвестны)
    Ready,      // Готов к началу (оба игрока известны)
    InProgress, // Идёт сейчас
    Completed   // Завершён
}

/// <summary>
/// Модель отдельного матча в турнирной сетке
/// Хранит связи между раундами (кто победитель — идёт дальше)
/// </summary>
[Table("tournament_match")]
public class TournamentMatch
{
    [Key]
    [Column("ID_match")]
    public int Id { get; set; }

    [ForeignKey("Tournament")]
    [Column("ID_tournament")]
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    // Ссылка на созданную игру (NULL пока не создана)
    [ForeignKey("Game")]
    [Column("ID_game")]
    public int? GameId { get; set; }
    public Game? Game { get; set; }

    // Номер раунда: 1 = первый раунд, 2 = четвертьфинал и т.д.
    [Column("round_number")]
    public int RoundNumber { get; set; }

    // Порядковый номер матча в раунде
    [Column("match_number")]
    public int MatchNumber { get; set; }

    // Первый игрок (NULL если пока неизвестен)
    [ForeignKey("Player1")]
    [Column("ID_player1")]
    public int? Player1Id { get; set; }
    public User? Player1 { get; set; }

    // Второй игрок
    [ForeignKey("Player2")]
    [Column("ID_player2")]
    public int? Player2Id { get; set; }
    public User? Player2 { get; set; }

    // Победитель матча (идёт дальше по сетке)
    [ForeignKey("Winner")]
    [Column("ID_winner")]
    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    // Ссылка на следующий матч (куда передаётся победитель)
    [ForeignKey("NextMatch")]
    [Column("ID_next_match")]
    public int? NextMatchId { get; set; }
    public TournamentMatch? NextMatch { get; set; }

    // Флаг финального матча
    [Column("is_final")]
    public bool IsFinal { get; set; } = false;

    // Когда запланирован матч
    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
}