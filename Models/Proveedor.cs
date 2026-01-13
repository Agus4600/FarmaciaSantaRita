using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FarmaciaSantaRita.Models;

public partial class Proveedor
{
    public int Idproveedor { get; set; }

    public string NombreProveedor { get; set; } = null!;

    public string EstadoProveedor { get; set; } = null!;

    [Required(ErrorMessage = "El Correo es obligatorio.")]
    public string CorreoProveedor { get; set; } = null!;

    public string TelefonoProveedor { get; set; } = null!;

    public bool Eliminado { get; set; }

    public virtual ICollection<Boletum> Boleta { get; set; } = new List<Boletum>();
}
