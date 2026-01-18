using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models
{
    [Table("Vacaciones")]  // Correcto: fuerza el nombre de la tabla
    public class Vacacion
    {
        [Key]  // ← Agrega esto para dejar claro cuál es la PK (EF lo infiere, pero es buena práctica)
        public int IdVacaciones { get; set; }

        [Column("IDUsuari")]  // ← ¡¡ESTO ES LA CLAVE!! Mapea la propiedad C# a la columna real de la BD
        public int Idusuario { get; set; }  // Nombre en C# puede ser Idusuario (camelCase)

        public int DiasVacaciones { get; set; }

        // DateTime sin ? es NOT NULL, lo cual coincide con tu BD (DATE NOT NULL)
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public int DiasFavor { get; set; }

        public string? NombreEmpleadoRegistrado { get; set; }  // NULLABLE ok

        [NotMapped]  // Correcto: no se guarda en BD, solo para el frontend
        public string? NombreEmpleadoFrontend { get; set; }

        [ForeignKey("Idusuario")]  // Correcto
        public virtual Usuario? Usuario { get; set; }
    }
}