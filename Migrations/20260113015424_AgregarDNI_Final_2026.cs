using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaciaSantaRita.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDNI_Final_2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreEmpleadoRegistrado",
                table: "Vacaciones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreEmpleadoRegistrado",
                table: "Vacaciones");
        }
    }
}
