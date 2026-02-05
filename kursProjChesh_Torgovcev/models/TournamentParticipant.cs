using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Связь между пользователем и турниром
/// Хранит информацию об участии (когда зарегистрировался, номер посева)
/// </summary>
[Table("tournament_participant")]
public class TournamentParticipant
{
    [Key]
    [Column("ID_participant")]
    public int Id { get; set; }

    [ForeignKey("Tournament")]
    [Column("ID_tournament")]
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [ForeignKey("User")]
    [Column("ID_user")]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.Now;

    // Номер посева — используется для распределения по сетке
    // Сильные игроки разносятся по разным частям турнирной сетки
    [Column("seed_number")]
    public int? SeedNumber { get; set; }
}