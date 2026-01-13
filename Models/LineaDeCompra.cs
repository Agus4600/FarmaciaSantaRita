using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class LineaDeCompra
{
    public int IdlineaDeCompra { get; set; }

    public int Idproducto { get; set; }

    public int Cantidad { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual Producto IdproductoNavigation { get; set; } = null!;
}
