using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models;

[Table("compra")]
public partial class Compra
{
    [Key]
    [Column("idcompras")]
    public int Idcompras { get; set; }

    [Column("idlineadecompra")] // ¡Aquí estaba el error! EF buscaba IDLineaDeCompra
    public int IdlineaDeCompra { get; set; }

    public string Descripcion { get; set; } = null!;
    public DateOnly FechaCompra { get; set; }
    public decimal MontoCompra { get; set; }

    [Column("idcliente")]
    public int Idcliente { get; set; }

    [Column("idusuario")]
    public int Idusuario { get; set; }

    public string EstadoDePago { get; set; }

    public virtual Cliente IdclienteNavigation { get; set; } = null!;
    public virtual LineaDeCompra IdlineaDeCompraNavigation { get; set; } = null!;
    public virtual Usuario IdusuarioNavigation { get; set; } = null!;
}