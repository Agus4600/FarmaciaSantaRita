using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class Producto
{
    public int Idproducto { get; set; }

    public string NombreProducto { get; set; } = null!;

    public decimal PrecioUnitario { get; set; }

    public virtual ICollection<LineaDeCompra> LineaDeCompras { get; set; } = new List<LineaDeCompra>();
}
