using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class LineaDeCompra
{
    public int Idlineadecompra { get; set; }
    public int Idcompras { get; set; } // Debe ser 'c' minúscula según tu error CS1061
    public int Idproducto { get; set; }
    public int Cantidad { get; set; }

    public virtual Compra IdcomprasNavigation { get; set; } = null!; 
    public virtual Producto IdproductoNavigation { get; set; } = null!;
}