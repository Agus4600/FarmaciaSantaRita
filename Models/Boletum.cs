using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class Boletum
{
    public int Idboleta { get; set; }

    public int Idusuario { get; set; }

    public int Idproveedor { get; set; }

    public DateTime Fecha { get; set; }

    public int ImporteFinal { get; set; }

    public string Transfer { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public string Detalle { get; set; } = null!;

    public virtual Proveedor IdproveedorNavigation { get; set; } = null!;

    public virtual Usuario IdusuarioNavigation { get; set; } = null!;
}
