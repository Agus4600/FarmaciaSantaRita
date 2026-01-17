using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmaciaSantaRita.Models;

[Table("lineadecompra")] // Asegúrate que el nombre de la tabla esté en minúsculas si así está en Postgres
public partial class LineaDeCompra
{
    [Key]
    [Column("idlineadecompra")] // Forzamos minúsculas para Postgres
    public int IdlineaDeCompra { get; set; }

    [Column("idproducto")]
    public int Idproducto { get; set; }

    public int Cantidad { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public virtual Producto IdproductoNavigation { get; set; } = null!;
}


