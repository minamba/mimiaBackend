using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PieceJointe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PieceJointe",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    message_id = table.Column<int>(type: "int", nullable: true),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    type_mime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    taille = table.Column<int>(type: "int", nullable: false),
                    nombre_pages = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceJointe", x => x.id);
                    table.ForeignKey(
                        name: "FK_PieceJointe_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PieceJointe_Message_message_id",
                        column: x => x.message_id,
                        principalTable: "Message",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PieceJointe_conversation_id_date_creation",
                table: "PieceJointe",
                columns: new[] { "conversation_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_PieceJointe_message_id",
                table: "PieceJointe",
                column: "message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PieceJointe");
        }
    }
}
