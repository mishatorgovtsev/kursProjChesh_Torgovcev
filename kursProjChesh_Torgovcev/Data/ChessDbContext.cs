using ChessPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace ChessPlatform.Data;


public class ChessDbContext : DbContext
{
    public ChessDbContext(DbContextOptions<ChessDbContext> options) : base(options) { }

    // Таблицы базы данных
    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameMove> GameMoves { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<TournamentMatch> TournamentMatches { get; set; }
    public DbSet<RatingHistory> RatingHistories { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ============================================
    // НАСТРОЙКА Tournament -> Creator
    // ============================================
    modelBuilder.Entity<Tournament>()
        .HasOne(t => t.Creator)
        .WithMany(u => u.CreatedTournaments)
        .HasForeignKey(t => t.CreatorId)
        .OnDelete(DeleteBehavior.Restrict);

    // ============================================
    // НАСТРОЙКА Tournament -> Winner
    // ============================================
    modelBuilder.Entity<Tournament>()
        .HasOne(t => t.Winner)
        .WithMany()
        .HasForeignKey(t => t.WinnerId)
        .OnDelete(DeleteBehavior.SetNull);

    // ============================================
    // НАСТРОЙКА TournamentMatch -> NextMatch
    // ============================================
    modelBuilder.Entity<TournamentMatch>()
        .HasOne(tm => tm.NextMatch)
        .WithMany()
        .HasForeignKey(tm => tm.NextMatchId)
        .OnDelete(DeleteBehavior.Restrict);

    // ============================================
    // ОСТАЛЬНЫЕ НАСТРОЙКИ (уже были)
    // ============================================
    
    // Username и Email должны быть уникальными
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Username)
        .IsUnique();
            
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();

    // Один пользователь — одно участие в турнире
    modelBuilder.Entity<TournamentParticipant>()
        .HasIndex(tp => new { tp.TournamentId, tp.UserId })
        .IsUnique();

    // Игра -> Белые
    modelBuilder.Entity<Game>()
        .HasOne(g => g.WhitePlayer)
        .WithMany(u => u.GamesAsWhite)
        .HasForeignKey(g => g.WhitePlayerId)
        .OnDelete(DeleteBehavior.Restrict);

    // Игра -> Чёрные
    modelBuilder.Entity<Game>()
        .HasOne(g => g.BlackPlayer)
        .WithMany(u => u.GamesAsBlack)
        .HasForeignKey(g => g.BlackPlayerId)
        .OnDelete(DeleteBehavior.Restrict);

    // Игра -> Победитель
    modelBuilder.Entity<Game>()
        .HasOne(g => g.Winner)
        .WithMany()
        .HasForeignKey(g => g.WinnerId)
        .OnDelete(DeleteBehavior.SetNull);

    // Ходы при удалении игры удаляются каскадом
    modelBuilder.Entity<GameMove>()
        .HasOne(m => m.Game)
        .WithMany(g => g.Moves)
        .OnDelete(DeleteBehavior.Cascade);

    // Участники турнира при удалении турнира удаляются каскадом
    modelBuilder.Entity<TournamentParticipant>()
        .HasOne(tp => tp.Tournament)
        .WithMany(t => t.Participants)
        .OnDelete(DeleteBehavior.Cascade);

    // Матчи турнира при удалении турнира удаляются каскадом
    modelBuilder.Entity<TournamentMatch>()
        .HasOne(tm => tm.Tournament)
        .WithMany(t => t.Matches)
        .OnDelete(DeleteBehavior.Cascade);
}
}