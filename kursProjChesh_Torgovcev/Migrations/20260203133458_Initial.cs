using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kursProjChesh_Torgovcev.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    ID_user = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    games_played = table.Column<int>(type: "int", nullable: false),
                    wins = table.Column<int>(type: "int", nullable: false),
                    losses = table.Column<int>(type: "int", nullable: false),
                    draws = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_online_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_account", x => x.ID_user);
                });

            migrationBuilder.CreateTable(
                name: "tournament",
                columns: table => new
                {
                    ID_tournament = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ID_creator = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    max_participants = table.Column<int>(type: "int", nullable: false),
                    current_participants = table.Column<int>(type: "int", nullable: false),
                    time_control_minutes = table.Column<int>(type: "int", nullable: false),
                    starts_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ID_winner = table.Column<int>(type: "int", nullable: true),
                    total_rounds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament", x => x.ID_tournament);
                    table.ForeignKey(
                        name: "FK_tournament_user_account_ID_creator",
                        column: x => x.ID_creator,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournament_user_account_ID_winner",
                        column: x => x.ID_winner,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "game",
                columns: table => new
                {
                    ID_game = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_white_player = table.Column<int>(type: "int", nullable: false),
                    ID_black_player = table.Column<int>(type: "int", nullable: false),
                    ID_winner = table.Column<int>(type: "int", nullable: true),
                    result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    time_control_minutes = table.Column<int>(type: "int", nullable: false),
                    time_control_increment = table.Column<int>(type: "int", nullable: false),
                    current_fen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    pgn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    white_time_remaining = table.Column<int>(type: "int", nullable: false),
                    black_time_remaining = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_move_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ID_tournament = table.Column<int>(type: "int", nullable: true),
                    tournament_round = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game", x => x.ID_game);
                    table.ForeignKey(
                        name: "FK_game_tournament_ID_tournament",
                        column: x => x.ID_tournament,
                        principalTable: "tournament",
                        principalColumn: "ID_tournament");
                    table.ForeignKey(
                        name: "FK_game_user_account_ID_black_player",
                        column: x => x.ID_black_player,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_user_account_ID_white_player",
                        column: x => x.ID_white_player,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_user_account_ID_winner",
                        column: x => x.ID_winner,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tournament_participant",
                columns: table => new
                {
                    ID_participant = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_tournament = table.Column<int>(type: "int", nullable: false),
                    ID_user = table.Column<int>(type: "int", nullable: false),
                    registered_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    seed_number = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_participant", x => x.ID_participant);
                    table.ForeignKey(
                        name: "FK_tournament_participant_tournament_ID_tournament",
                        column: x => x.ID_tournament,
                        principalTable: "tournament",
                        principalColumn: "ID_tournament",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tournament_participant_user_account_ID_user",
                        column: x => x.ID_user,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_move",
                columns: table => new
                {
                    ID_move = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_game = table.Column<int>(type: "int", nullable: false),
                    move_number = table.Column<int>(type: "int", nullable: false),
                    player_color = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    move_notation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    fen_after_move = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    made_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_move", x => x.ID_move);
                    table.ForeignKey(
                        name: "FK_game_move_game_ID_game",
                        column: x => x.ID_game,
                        principalTable: "game",
                        principalColumn: "ID_game",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rating_history",
                columns: table => new
                {
                    ID_history = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_user = table.Column<int>(type: "int", nullable: false),
                    ID_game = table.Column<int>(type: "int", nullable: false),
                    old_rating = table.Column<int>(type: "int", nullable: false),
                    new_rating = table.Column<int>(type: "int", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_history", x => x.ID_history);
                    table.ForeignKey(
                        name: "FK_rating_history_game_ID_game",
                        column: x => x.ID_game,
                        principalTable: "game",
                        principalColumn: "ID_game",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rating_history_user_account_ID_user",
                        column: x => x.ID_user,
                        principalTable: "user_account",
                        principalColumn: "ID_user",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_match",
                columns: table => new
                {
                    ID_match = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ID_tournament = table.Column<int>(type: "int", nullable: false),
                    ID_game = table.Column<int>(type: "int", nullable: true),
                    round_number = table.Column<int>(type: "int", nullable: false),
                    match_number = table.Column<int>(type: "int", nullable: false),
                    ID_player1 = table.Column<int>(type: "int", nullable: true),
                    ID_player2 = table.Column<int>(type: "int", nullable: true),
                    ID_winner = table.Column<int>(type: "int", nullable: true),
                    ID_next_match = table.Column<int>(type: "int", nullable: true),
                    is_final = table.Column<bool>(type: "bit", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_match", x => x.ID_match);
                    table.ForeignKey(
                        name: "FK_tournament_match_game_ID_game",
                        column: x => x.ID_game,
                        principalTable: "game",
                        principalColumn: "ID_game");
                    table.ForeignKey(
                        name: "FK_tournament_match_tournament_ID_tournament",
                        column: x => x.ID_tournament,
                        principalTable: "tournament",
                        principalColumn: "ID_tournament",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tournament_match_tournament_match_ID_next_match",
                        column: x => x.ID_next_match,
                        principalTable: "tournament_match",
                        principalColumn: "ID_match",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournament_match_user_account_ID_player1",
                        column: x => x.ID_player1,
                        principalTable: "user_account",
                        principalColumn: "ID_user");
                    table.ForeignKey(
                        name: "FK_tournament_match_user_account_ID_player2",
                        column: x => x.ID_player2,
                        principalTable: "user_account",
                        principalColumn: "ID_user");
                    table.ForeignKey(
                        name: "FK_tournament_match_user_account_ID_winner",
                        column: x => x.ID_winner,
                        principalTable: "user_account",
                        principalColumn: "ID_user");
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_ID_black_player",
                table: "game",
                column: "ID_black_player");

            migrationBuilder.CreateIndex(
                name: "IX_game_ID_tournament",
                table: "game",
                column: "ID_tournament");

            migrationBuilder.CreateIndex(
                name: "IX_game_ID_white_player",
                table: "game",
                column: "ID_white_player");

            migrationBuilder.CreateIndex(
                name: "IX_game_ID_winner",
                table: "game",
                column: "ID_winner");

            migrationBuilder.CreateIndex(
                name: "IX_game_move_ID_game",
                table: "game_move",
                column: "ID_game");

            migrationBuilder.CreateIndex(
                name: "IX_rating_history_ID_game",
                table: "rating_history",
                column: "ID_game");

            migrationBuilder.CreateIndex(
                name: "IX_rating_history_ID_user",
                table: "rating_history",
                column: "ID_user");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_ID_creator",
                table: "tournament",
                column: "ID_creator");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_ID_winner",
                table: "tournament",
                column: "ID_winner");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_game",
                table: "tournament_match",
                column: "ID_game");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_next_match",
                table: "tournament_match",
                column: "ID_next_match");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_player1",
                table: "tournament_match",
                column: "ID_player1");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_player2",
                table: "tournament_match",
                column: "ID_player2");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_tournament",
                table: "tournament_match",
                column: "ID_tournament");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_match_ID_winner",
                table: "tournament_match",
                column: "ID_winner");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_participant_ID_tournament_ID_user",
                table: "tournament_participant",
                columns: new[] { "ID_tournament", "ID_user" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_participant_ID_user",
                table: "tournament_participant",
                column: "ID_user");

            migrationBuilder.CreateIndex(
                name: "IX_user_account_email",
                table: "user_account",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_account_username",
                table: "user_account",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_move");

            migrationBuilder.DropTable(
                name: "rating_history");

            migrationBuilder.DropTable(
                name: "tournament_match");

            migrationBuilder.DropTable(
                name: "tournament_participant");

            migrationBuilder.DropTable(
                name: "game");

            migrationBuilder.DropTable(
                name: "tournament");

            migrationBuilder.DropTable(
                name: "user_account");
        }
    }
}
