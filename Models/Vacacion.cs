using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ← Asegurate de tener este using

namespace FarmaciaSantaRita.Models
{
    [Table("Vacaciones")] // ← ¡¡ESTA LÍNEA ES LA CLAVE!!
    public class Vacacion
    {
        public int IdVacaciones { get; set; }
        public int Idusuario { get; set; }
        public int DiasVacaciones { get; set; }

        // Usar DateTime? (con signo de pregunta) permite nulos sin que explote la app
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public int DiasFavor { get; set; }

        // El nombre que viene de la BD
        public string? NombreEmpleadoRegistrado { get; set; }

        // Esta propiedad la usas para recibir el dato del JS
        [NotMapped]
        public string? NombreEmpleadoFrontend { get; set; }

        [ForeignKey("Idusuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}