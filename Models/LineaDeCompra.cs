using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class LineaDeCompra
{
    public int IdlineaDeCompra { get; set; }

    // Añadimos la FK que está en tu base de datos
    public int Idcompras { get; set; }

    public int Idproducto { get; set; }

    public int Cantidad { get; set; }

    // Cambiamos la colección por una navegación simple a Compra
    public virtual Compra IdcomprasNavigation { get; set; } = null!;

    public virtual Producto IdproductoNavigation { get; set; } = null!;
}