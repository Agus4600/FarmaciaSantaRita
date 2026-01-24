using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models
{
    public partial class Usuario
    {
        public int Idusuario { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string NombreUsuario { get; set; } = null!;

        [Required]
        public string Apellido { get; set; } = null!;

        public string NombreCompleto
        {
            get { return $"{Nombre} {Apellido}"; }
        }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        public string Direccion { get; set; } = null!;

        [Required]
        public string Dni { get; set; } = null!;

        [Required]
        public string Telefono { get; set; } = null!;

        [Required]
        public string Rol { get; set; } = null!;

        // ✅ VERSIÓN FINAL: La propiedad se llama exactamente igual que la columna en la BD
        // EF la mapeará automáticamente a la columna "Contraseña" existente
        public string? Contraseña { get; set; }

        // ✅ PROPIEDAD TEMPORAL para recibir la contraseña del formulario
        [NotMapped]
        // Solo para el formulario de registro. NO usar [Required] aquí porque rompe la edición.
        public string ContraseñaPlana { get; set; } = null!;

        [NotMapped]
        public string NuevaContraseña { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo no válido")]
        [RegularExpression(
           @"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|hotmail\.com|yahoo\.com|mail\.com|empresa\.org|universidad\.edu|pais\.co\.uk)$",
           ErrorMessage = "Solo se permiten correos de los dominios: gmail, outlook, hotmail, yahoo, mail, empresa.org, universidad.edu, pais.co.uk")]
        public string CorreoUsuario { get; set; } = null!;

        public DateTime? FechaIngreso { get; set; }

        public virtual ICollection<Boletum> Boleta { get; set; } = new List<Boletum>();
        public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
        public virtual ICollection<Inasistencium> Inasistencia { get; set; } = new List<Inasistencium>();

        public bool Eliminado { get; set; }
    }
}