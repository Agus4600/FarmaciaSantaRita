using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models
{
    [Table("Vacaciones")]
    public class Vacacion
    {
        [Key]
        public int IdVacaciones { get; set; }

        [Column("IDUsuari")]  // ← ESTA LÍNEA ES LA CLAVE: fuerza el mapeo a la columna real de la BD
        public int Idusuario { get; set; }  // Nombre en C# puede seguir siendo Idusuario

        public int DiasVacaciones { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public int DiasFavor { get; set; }

        public string? NombreEmpleadoRegistrado { get; set; }

        [NotMapped]
        public string? NombreEmpleadoFrontend { get; set; }

        [ForeignKey("Idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}