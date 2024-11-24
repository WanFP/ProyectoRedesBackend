using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaminaDOSApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVotosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Votos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RondaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JugadorNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votos_Rondas_RondaId",
                        column: x => x.RondaId,
                        principalTable: "Rondas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_RondaId",
                table: "Votos",
                column: "RondaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votos");
        }
    }
}
