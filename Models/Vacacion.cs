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

        [Column("IDUsuari")]  // ← ESTA LÍNEA ES LA QUE FALTA: fuerza el mapeo a la columna real "IDUsuari" de la BD
        public int Idusuario { get; set; }  // El nombre en C# puede seguir siendo Idusuario (camelCase)

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