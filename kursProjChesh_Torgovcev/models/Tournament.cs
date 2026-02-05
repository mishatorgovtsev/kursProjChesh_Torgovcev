using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Статусы турнира
/// </summary>
public enum TournamentStatusEnum
{
    Registration,   // Открыта регистрация
    InProgress,     // Турнир идёт
    Completed,      // Завершён
    Cancelled       // Отменён
}

/// <summary>
/// Модель турнира по олимпийской системе
/// Создаётся любым пользователем, имеет сетку на выбывание
/// </summary>
[Table("tournament")]
public class Tournament
{
    [Key]
    [Column("ID_tournament")]
    public int Id { get; set; }

    [Required]
    [Column("title")]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    // Кто создал турнир
    [ForeignKey("Creator")]
    [Column("ID_creator")]
    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    // Текущий статус
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Registration";

    // Максимум участников (степень двойки: 4, 8, 16, 32, 64)
    [Column("max_participants")]
    public int MaxParticipants { get; set; } = 32;

    // Текущее количество зарегистрировавшихся
    [Column("current_participants")]
    public int CurrentParticipants { get; set; } = 0;

    // Контроль времени для всех игр турнира
    [Column("time_control_minutes")]
    public int TimeControlMinutes { get; set; } = 90;

    // Когда турнир начнётся
    [Column("starts_at")]
    public DateTime StartsAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Победитель турнира
    [ForeignKey("Winner")]
    [Column("ID_winner")]
    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    // Общее количество раундов (log2 от max_participants)
    [Column("total_rounds")]
    public int? TotalRounds { get; set; }

    // Навигационные свойства
    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<Game> Games { get; set; } = new List<Game>();
}