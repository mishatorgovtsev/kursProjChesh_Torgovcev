using ChessPlatform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace kursProjChesh_Torgovcev.Migrations
{
    [DbContext(typeof(ChessDbContext))]
    partial class ChessDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("User", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnName("ID_user");
                b.Property<string>("Username").HasMaxLength(50).HasColumnName("username");
                b.Property<string>("Email").HasMaxLength(100).HasColumnName("email");
                b.Property<string>("PasswordHash").HasMaxLength(255).HasColumnName("password_hash");
                b.Property<int>("Rating").HasDefaultValue(1200).HasColumnName("rating");
                b.Property<int>("GamesPlayed").HasDefaultValue(0).HasColumnName("games_played");
                b.Property<int>("Wins").HasDefaultValue(0).HasColumnName("wins");
                b.Property<int>("Losses").HasDefaultValue(0).HasColumnName("losses");
                b.Property<int>("Draws").HasDefaultValue(0).HasColumnName("draws");
                b.Property<DateTime>("CreatedAt").HasColumnName("created_at");
                b.Property<DateTime?>("LastOnlineAt").HasColumnName("last_online_at");
                b.Property<bool>("IsActive").HasDefaultValue(true).HasColumnName("is_active");
                b.HasKey("Id");
                b.ToTable("user_account");
            });
#pragma warning restore 612, 618
        }
    }
}
