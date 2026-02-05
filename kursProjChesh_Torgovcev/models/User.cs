using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Модель пользователя (игрока)
/// Хранит данные аккаунта, рейтинг ELO и статистику
/// </summary>
[Table("user_account")]
public class User
{
    [Key]
    [Column("ID_user")]
    public int Id { get; set; }

    [Required]
    [Column("username")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // Рейтинг ELO (начальный 1200)
    [Column("rating")]
    public int Rating { get; set; } = 1200;

    [Column("games_played")]
    public int GamesPlayed { get; set; } = 0;

    [Column("wins")]
    public int Wins { get; set; } = 0;

    [Column("losses")]
    public int Losses { get; set; } = 0;

    [Column("draws")]
    public int Draws { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("last_online_at")]
    public DateTime? LastOnlineAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    public ICollection<Game> GamesAsWhite { get; set; } = new List<Game>();
    public ICollection<Game> GamesAsBlack { get; set; } = new List<Game>();
    public ICollection<Tournament> CreatedTournaments { get; set; } = new List<Tournament>();
    public ICollection<TournamentParticipant> TournamentParticipations { get; set; } = new List<TournamentParticipant>();
    public ICollection<RatingHistory> RatingChanges { get; set; } = new List<RatingHistory>();
}