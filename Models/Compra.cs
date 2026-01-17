using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class Compra
{
    public int Idcompras { get; set; }

    // Borramos IdlineaDeCompra porque la FK está en la otra tabla

    public string Descripcion { get; set; } = null!;

    public DateOnly FechaCompra { get; set; }

    public decimal MontoCompra { get; set; }

    public int Idcliente { get; set; }

    public int Idusuario { get; set; }

    public string EstadoDePago { get; set; }

    public virtual Cliente IdclienteNavigation { get; set; } = null!;

    public virtual Usuario IdusuarioNavigation { get; set; } = null!;

    // Ahora la compra tiene una lista de líneas
    public virtual ICollection<LineaDeCompra> LineaDeCompras { get; set; } = new List<LineaDeCompra>();
}