using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContaminaDOSApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Juegos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LiderActualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecadaActual = table.Column<int>(type: "int", nullable: false),
                    PuntosCiudadanos = table.Column<int>(type: "int", nullable: false),
                    PuntosPsicopatas = table.Column<int>(type: "int", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Psicopata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Juegos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jugadores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JuegoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jugadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jugadores_Juegos_JuegoId",
                        column: x => x.JuegoId,
                        principalTable: "Juegos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rondas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fase = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Votos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Acciones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JugadoresQueYaVotaron = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JugadoresQueYaTomaronAccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntentosDePropuesta = table.Column<int>(type: "int", nullable: false),
                    JuegoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rondas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rondas_Juegos_JuegoId",
                        column: x => x.JuegoId,
                        principalTable: "Juegos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Juegos_LiderActualId",
                table: "Juegos",
                column: "LiderActualId");

            migrationBuilder.CreateIndex(
                name: "IX_Jugadores_JuegoId",
                table: "Jugadores",
                column: "JuegoId");

            migrationBuilder.CreateIndex(
                name: "IX_Rondas_JuegoId",
                table: "Rondas",
                column: "JuegoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Juegos_Jugadores_LiderActualId",
                table: "Juegos",
                column: "LiderActualId",
                principalTable: "Jugadores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Juegos_Jugadores_LiderActualId",
                table: "Juegos");

            migrationBuilder.DropTable(
                name: "Rondas");

            migrationBuilder.DropTable(
                name: "Jugadores");

            migrationBuilder.DropTable(
                name: "Juegos");
        }
    }
}
