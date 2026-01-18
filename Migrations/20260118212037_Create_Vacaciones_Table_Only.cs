using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FarmaciaSantaRita.Migrations
{
    /// <inheritdoc />
    public partial class Create_Vacaciones_Table_Only : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    IDCliente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCliente = table.Column<string>(type: "text", nullable: false),
                    TelefonoCliente = table.Column<string>(type: "text", nullable: false),
                    DireccionCliente = table.Column<string>(type: "text", nullable: false),
                    DNI = table.Column<string>(type: "text", nullable: true),
                    EstadoPago = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.IDCliente);
                });

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    IDProducto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreProducto = table.Column<string>(type: "text", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.IDProducto);
                });

            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    IDProveedor = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreProveedor = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    EstadoProveedor = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    CorreoProveedor = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    TelefonoProveedor = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedor", x => x.IDProveedor);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IDUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    NombreUsuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Direccion = table.Column<string>(type: "character varying(150)", unicode: false, maxLength: 150, nullable: false),
                    DNI = table.Column<string>(type: "character(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Contraseña = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    CorreoUsuario = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IDUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Boleta",
                columns: table => new
                {
                    IDBoleta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuario = table.Column<int>(type: "integer", nullable: false),
                    IDProveedor = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImporteFinal = table.Column<int>(type: "integer", nullable: false),
                    Transfer = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boleta", x => x.IDBoleta);
                    table.ForeignKey(
                        name: "FK_Boleta_Proveedor",
                        column: x => x.IDProveedor,
                        principalTable: "Proveedor",
                        principalColumn: "IDProveedor");
                    table.ForeignKey(
                        name: "FK_Boleta_Usuarios",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IDUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    IDCompras = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "text", unicode: false, nullable: false),
                    FechaCompra = table.Column<DateOnly>(type: "date", nullable: false),
                    MontoCompra = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IDCliente = table.Column<int>(type: "integer", nullable: false),
                    IDUsuario = table.Column<int>(type: "integer", nullable: false),
                    EstadoDePago = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.IDCompras);
                    table.ForeignKey(
                        name: "FK_Compras_Clientes",
                        column: x => x.IDCliente,
                        principalTable: "Clientes",
                        principalColumn: "IDCliente");
                    table.ForeignKey(
                        name: "FK_Compras_Usuarios",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IDUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Inasistencia",
                columns: table => new
                {
                    IDInasistencias = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuario = table.Column<int>(type: "integer", nullable: false),
                    NombreEmpleado = table.Column<string>(type: "text", unicode: false, nullable: false),
                    Motivo = table.Column<string>(type: "text", unicode: false, nullable: false),
                    FechaInasistencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Turno = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inasistencia", x => x.IDInasistencias);
                    table.ForeignKey(
                        name: "FK_Inasistencia_Usuarios",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IDUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Vacaciones",
                columns: table => new
                {
                    IdVacaciones = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDUsuario = table.Column<int>(type: "integer", nullable: false),
                    DiasVacaciones = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiasFavor = table.Column<int>(type: "integer", nullable: false),
                    NombreEmpleadoRegistrado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacaciones", x => x.IdVacaciones);
                    table.ForeignKey(
                        name: "FK_Vacaciones_Usuarios",
                        column: x => x.IDUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IDUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineaDeCompra",
                columns: table => new
                {
                    IDLineaDeCompra = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IDCompras = table.Column<int>(type: "integer", nullable: false),
                    IDProducto = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineaDeCompra", x => x.IDLineaDeCompra);
                    table.ForeignKey(
                        name: "FK_LineaDeCompra_Compras",
                        column: x => x.IDCompras,
                        principalTable: "Compras",
                        principalColumn: "IDCompras",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineaDeCompra_Producto_IDProducto",
                        column: x => x.IDProducto,
                        principalTable: "Producto",
                        principalColumn: "IDProducto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boleta_IDProveedor",
                table: "Boleta",
                column: "IDProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_Boleta_IDUsuario",
                table: "Boleta",
                column: "IDUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_IDCliente",
                table: "Compras",
                column: "IDCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_IDUsuario",
                table: "Compras",
                column: "IDUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Inasistencia_IDUsuario",
                table: "Inasistencia",
                column: "IDUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_LineaDeCompra_IDCompras",
                table: "LineaDeCompra",
                column: "IDCompras");

            migrationBuilder.CreateIndex(
                name: "IX_LineaDeCompra_IDProducto",
                table: "LineaDeCompra",
                column: "IDProducto");

            migrationBuilder.CreateIndex(
                name: "IX_Vacaciones_IDUsuario",
                table: "Vacaciones",
                column: "IDUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boleta");

            migrationBuilder.DropTable(
                name: "Inasistencia");

            migrationBuilder.DropTable(
                name: "LineaDeCompra");

            migrationBuilder.DropTable(
                name: "Vacaciones");

            migrationBuilder.DropTable(
                name: "Proveedor");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
