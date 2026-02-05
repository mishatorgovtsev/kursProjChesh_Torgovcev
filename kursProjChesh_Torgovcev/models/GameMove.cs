using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChessPlatform.Models;

/// <summary>
/// Модель отдельного хода в партии
/// Нужна для воспроизведения игры и анализа
/// </summary>
[Table("game_move")]
public class GameMove
{
    [Key]
    [Column("ID_move")]
    public int Id { get; set; }

    // К какой игре относится ход
    [ForeignKey("Game")]
    [Column("ID_game")]
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    // Номер хода (1, 2, 3...)
    [Column("move_number")]
    public int MoveNumber { get; set; }

    // Цвет игрока: 'w' (white) или 'b' (black)
    [Column("player_color")]
    [MaxLength(1)]
    public string PlayerColor { get; set; } = string.Empty;

    // Ход в алгебраической нотации
    // Примеры: "e4", "Nf3", "O-O" (рокировка), "exd5" (взятие)
    [Column("move_notation")]
    [MaxLength(10)]
    public string MoveNotation { get; set; } = string.Empty;

    // Позиция на доске ПОСЛЕ хода (FEN)
    [Column("fen_after_move")]
    [MaxLength(100)]
    public string FENAfterMove { get; set; } = string.Empty;

    [Column("made_at")]
    public DateTime MadeAt { get; set; } = DateTime.Now;
}