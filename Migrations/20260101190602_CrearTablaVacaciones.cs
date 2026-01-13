using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaciaSantaRita.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaVacaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vacaciones",
                columns: table => new
                {
                    IdVacaciones = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Idusuario = table.Column<int>(type: "int", nullable: false),
                    DiasVacaciones = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiasFavor = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacaciones", x => x.IdVacaciones);
                    table.ForeignKey(
                        name: "FK_Vacaciones_Usuarios_Idusuario",
                        column: x => x.Idusuario,
                        principalTable: "Usuarios",
                        principalColumn: "Idusuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacaciones_Idusuario",
                table: "Vacaciones",
                column: "Idusuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Vacaciones");
        }
    }
}
