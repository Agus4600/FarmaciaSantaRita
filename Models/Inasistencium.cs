using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models;
[Table("Inasistencia")]

public partial class Inasistencium
{
    public int Idinasistencias { get; set; }

    public int Idusuario { get; set; }

    public string NombreEmpleado { get; set; } = null!;

    public string Motivo { get; set; } = null!;

    public DateOnly FechaInasistencia { get; set; }

    public string Turno { get; set; } = null!;
    public virtual Usuario IdusuarioNavigation { get; set; } = null!;

}
