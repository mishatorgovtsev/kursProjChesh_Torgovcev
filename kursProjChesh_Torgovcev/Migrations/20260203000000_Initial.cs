using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kursProjChesh_Torgovcev.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    ID_user          = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    username         = table.Column<string>(maxLength: 50,  nullable: false),
                    email            = table.Column<string>(maxLength: 100, nullable: false),
                    password_hash    = table.Column<string>(maxLength: 255, nullable: false),
                    rating           = table.Column<int>(nullable: false, defaultValue: 1200),
                    games_played     = table.Column<int>(nullable: false, defaultValue: 0),
                    wins             = table.Column<int>(nullable: false, defaultValue: 0),
                    losses           = table.Column<int>(nullable: false, defaultValue: 0),
                    draws            = table.Column<int>(nullable: false, defaultValue: 0),
                    created_at       = table.Column<DateTime>(nullable: false),
                    last_online_at   = table.Column<DateTime>(nullable: true),
                    is_active        = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table => { table.PrimaryKey("PK_user_account", x => x.ID_user); });

            migrationBuilder.CreateIndex("IX_user_account_username", "user_account", "username", unique: true);
            migrationBuilder.CreateIndex("IX_user_account_email",    "user_account", "email",    unique: true);

            migrationBuilder.CreateTable(
                name: "tournament",
                columns: table => new
                {
                    ID_tournament        = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    title                = table.Column<string>(maxLength: 100, nullable: false),
                    description          = table.Column<string>(maxLength: 500, nullable: true),
                    ID_creator           = table.Column<int>(nullable: false),
                    status               = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Registration"),
                    max_participants     = table.Column<int>(nullable: false, defaultValue: 32),
                    current_participants = table.Column<int>(nullable: false, defaultValue: 0),
                    time_control_minutes = table.Column<int>(nullable: false, defaultValue: 90),
                    starts_at            = table.Column<DateTime>(nullable: false),
                    created_at           = table.Column<DateTime>(nullable: false),
                    ID_winner            = table.Column<int>(nullable: true),
                    total_rounds         = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament", x => x.ID_tournament);
                    table.ForeignKey("FK_tournament_creator", x => x.ID_creator, "user_account", "ID_user", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_tournament_winner",  x => x.ID_winner,  "user_account", "ID_user", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "game",
                columns: table => new
                {
                    ID_game              = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ID_white_player      = table.Column<int>(nullable: false),
                    ID_black_player      = table.Column<int>(nullable: false),
                    ID_winner            = table.Column<int>(nullable: true),
                    result               = table.Column<string>(maxLength: 20, nullable: true),
                    status               = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Pending"),
                    time_control_minutes = table.Column<int>(nullable: false, defaultValue: 10),
                    time_control_increment = table.Column<int>(nullable: false, defaultValue: 0),
                    current_fen          = table.Column<string>(maxLength: 100, nullable: false),
                    pgn                  = table.Column<string>(nullable: true),
                    white_time_remaining = table.Column<int>(nullable: false, defaultValue: 600),
                    black_time_remaining = table.Column<int>(nullable: false, defaultValue: 600),
                    started_at           = table.Column<DateTime>(nullable: true),
                    last_move_at         = table.Column<DateTime>(nullable: true),
                    ended_at             = table.Column<DateTime>(nullable: true),
                    created_at           = table.Column<DateTime>(nullable: false),
                    ID_tournament        = table.Column<int>(nullable: true),
                    tournament_round     = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game", x => x.ID_game);
                    table.ForeignKey("FK_game_white",      x => x.ID_white_player, "user_account", "ID_user", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_game_black",      x => x.ID_black_player, "user_account", "ID_user", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_game_winner",     x => x.ID_winner,       "user_account", "ID_user", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_game_tournament", x => x.ID_tournament,   "tournament",   "ID_tournament", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "game_move",
                columns: table => new
                {
                    ID_move        = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ID_game        = table.Column<int>(nullable: false),
                    move_number    = table.Column<int>(nullable: false),
                    player_color   = table.Column<string>(maxLength: 1, nullable: false),
                    move_notation  = table.Column<string>(maxLength: 10, nullable: false),
                    fen_after_move = table.Column<string>(maxLength: 100, nullable: false),
                    made_at        = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_move", x => x.ID_move);
                    table.ForeignKey("FK_game_move_game", x => x.ID_game, "game", "ID_game", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rating_history",
                columns: table => new
                {
                    ID_history    = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ID_user       = table.Column<int>(nullable: false),
                    ID_game       = table.Column<int>(nullable: false),
                    old_rating    = table.Column<int>(nullable: false),
                    new_rating    = table.Column<int>(nullable: false),
                    rating_change = table.Column<int>(nullable: false),
                    recorded_at   = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_history", x => x.ID_history);
                    table.ForeignKey("FK_rating_user", x => x.ID_user, "user_account", "ID_user", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_rating_game", x => x.ID_game, "game",         "ID_game", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_participant",
                columns: table => new
                {
                    ID_participant = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ID_tournament  = table.Column<int>(nullable: false),
                    ID_user        = table.Column<int>(nullable: false),
                    registered_at  = table.Column<DateTime>(nullable: false),
                    seed_number    = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_participant", x => x.ID_participant);
                    table.ForeignKey("FK_participant_tournament", x => x.ID_tournament, "tournament",    "ID_tournament", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_participant_user",       x => x.ID_user,       "user_account",  "ID_user",       onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_tournament_participant_unique", "tournament_participant",
                new[] { "ID_tournament", "ID_user" }, unique: true);

            migrationBuilder.CreateTable(
                name: "tournament_match",
                columns: table => new
                {
                    ID_match      = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ID_tournament = table.Column<int>(nullable: false),
                    ID_game       = table.Column<int>(nullable: true),
                    round_number  = table.Column<int>(nullable: false),
                    match_number  = table.Column<int>(nullable: false),
                    ID_player1    = table.Column<int>(nullable: true),
                    ID_player2    = table.Column<int>(nullable: true),
                    ID_winner     = table.Column<int>(nullable: true),
                    ID_next_match = table.Column<int>(nullable: true),
                    is_final      = table.Column<bool>(nullable: false, defaultValue: false),
                    scheduled_at  = table.Column<DateTime>(nullable: true),
                    status        = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_match", x => x.ID_match);
                    table.ForeignKey("FK_match_tournament", x => x.ID_tournament, "tournament",      "ID_tournament", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_match_game",       x => x.ID_game,       "game",            "ID_game",       onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_match_player1",    x => x.ID_player1,    "user_account",    "ID_user",       onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_match_player2",    x => x.ID_player2,    "user_account",    "ID_user",       onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_match_winner",     x => x.ID_winner,     "user_account",    "ID_user",       onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_match_next",       x => x.ID_next_match, "tournament_match","ID_match",      onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("tournament_match");
            migrationBuilder.DropTable("tournament_participant");
            migrationBuilder.DropTable("rating_history");
            migrationBuilder.DropTable("game_move");
            migrationBuilder.DropTable("game");
            migrationBuilder.DropTable("tournament");
            migrationBuilder.DropTable("user_account");
        }
    }
}
