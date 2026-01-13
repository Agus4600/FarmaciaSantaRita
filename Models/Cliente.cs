using System;
using System.Collections.Generic;

namespace FarmaciaSantaRita.Models;

public partial class Cliente
{
    public int Idcliente { get; set; }

    public string NombreCliente { get; set; } = null!;

    public string TelefonoCliente { get; set; } = null!;

    public string DireccionCliente { get; set; } = null!;
    public string? DNI { get; set; }

    public string EstadoDePago { get; set; } = null!;

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
