using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaminaDOSApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAccionesUpdateVotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acciones",
                table: "Rondas");

            migrationBuilder.DropColumn(
                name: "Votos",
                table: "Rondas");

            migrationBuilder.CreateTable(
                name: "Acciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RondaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JugadorNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Acciones_Rondas_RondaId",
                        column: x => x.RondaId,
                        principalTable: "Rondas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acciones_RondaId",
                table: "Acciones",
                column: "RondaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acciones");

            migrationBuilder.AddColumn<string>(
                name: "Acciones",
                table: "Rondas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Votos",
                table: "Rondas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
