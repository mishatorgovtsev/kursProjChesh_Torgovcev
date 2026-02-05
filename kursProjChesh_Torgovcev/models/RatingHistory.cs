using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// История изменения ELO рейтинга пользователя
/// Каждая игра порождает две записи (для белых и для чёрных)
/// </summary>
[Table("rating_history")]
public class RatingHistory
{
    [Key]
    [Column("ID_history")]
    public int Id { get; set; }

    // Чей рейтинг изменился
    [ForeignKey("User")]
    [Column("ID_user")]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Из-за какой игры
    [ForeignKey("Game")]
    [Column("ID_game")]
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    // Старый рейтинг (до игры)
    [Column("old_rating")]
    public int OldRating { get; set; }

    // Новый рейтинг (после игры)
    [Column("new_rating")]
    public int NewRating { get; set; }

    // Вычисляемое поле: изменение (может быть отрицательным)
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column("rating_change")]
    public int RatingChange => NewRating - OldRating;

    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}