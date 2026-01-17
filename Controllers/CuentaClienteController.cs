using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FarmaciaSantaRita.Controllers
{
    [Authorize]
    public class CuentaClienteController : Controller
    {
        private readonly FarmaciabdContext _context;

        public CuentaClienteController(FarmaciabdContext context)
        {
            _context = context;
        }

        public class CompraDto
        {
            public int idCompra { get; set; }
            public string nombre { get; set; }
            public string dni { get; set; }
            public string producto { get; set; }
            public int cantidad { get; set; }
            public decimal precio { get; set; }
            public string estado { get; set; }
            public string telefono { get; set; }
            public string direccion { get; set; }
            public string fecha { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Obtenemos las compras con todas sus relaciones
            var comprasBD = await _context.Compras
                .Include(c => c.IdclienteNavigation)
                .Include(c => c.LineaDeCompras)
                    .ThenInclude(l => l.IdproductoNavigation)
                .OrderByDescending(c => c.Idcompras)
                .ToListAsync();

            // 2. Usamos ViewBag para los datos de apoyo (combos/selects)
            // Usamos Select para enviar solo lo necesario y ahorrar memoria
            ViewBag.Clientes = await _context.Clientes
                .Select(c => new {
                    id = c.Idcliente,
                    nombre = c.NombreCliente,
                    dni = c.DNI
                }).ToListAsync();

            ViewBag.Productos = await _context.Productos.ToListAsync();

            // 3. Retornamos la lista de compras como el MODELO principal
            // Esto hace que en la vista puedas usar @model IEnumerable<Compra>
            return View(comprasBD);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNuevaCompra([FromBody] CompraDto datos)
        {
            if (datos == null) return Json(new { success = false, message = "Datos nulos" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Manejo del Cliente
                Cliente cliente = null;
                if (!string.IsNullOrEmpty(datos.dni))
                {
                    cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.DNI == datos.dni.Trim());
                    if (cliente != null)
                    {
                        cliente.NombreCliente = datos.nombre?.Trim() ?? cliente.NombreCliente;
                        if (!string.IsNullOrEmpty(datos.telefono)) cliente.TelefonoCliente = datos.telefono.Trim();
                        if (!string.IsNullOrEmpty(datos.direccion)) cliente.DireccionCliente = datos.direccion.Trim();
                        _context.Clientes.Update(cliente);
                    }
                }

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        NombreCliente = datos.nombre?.Trim() ?? "Sin nombre",
                        DNI = datos.dni?.Trim(),
                        TelefonoCliente = string.IsNullOrEmpty(datos.telefono) ? "Sin especificar" : datos.telefono.Trim(),
                        DireccionCliente = string.IsNullOrEmpty(datos.direccion) ? "Sin especificar" : datos.direccion.Trim(),
                        EstadoDePago = "Pendiente"
                    };
                    _context.Clientes.Add(cliente);
                }
                await _context.SaveChangesAsync();

                // 2. Manejo del Producto
                var nombreProdNorm = datos.producto.ToLower().Trim();
                var prod = await _context.Productos.FirstOrDefaultAsync(p => p.NombreProducto.ToLower() == nombreProdNorm);

                if (prod == null)
                {
                    prod = new Producto { NombreProducto = datos.producto.Trim(), PrecioUnitario = datos.precio };
                    _context.Productos.Add(prod);
                }
                else
                {
                    prod.PrecioUnitario = datos.precio;
                    _context.Productos.Update(prod);
                }
                await _context.SaveChangesAsync();

                // 3. Crear la COMPRA primero (porque LineaDeCompra necesita el ID de Compra)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int idUsuarioLogueado = string.IsNullOrEmpty(userIdClaim) ? 1 : int.Parse(userIdClaim);

                DateOnly fechaCompra = DateOnly.TryParse(datos.fecha, out DateOnly parsedFecha)
                    ? parsedFecha : DateOnly.FromDateTime(DateTime.Now);

                var nuevaCompra = new Compra
                {
                    Descripcion = $"Venta de {datos.producto}",
                    FechaCompra = fechaCompra,
                    MontoCompra = datos.cantidad * datos.precio,
                    Idcliente = cliente.Idcliente,
                    Idusuario = idUsuarioLogueado,
                    EstadoDePago = "Pendiente"
                };

                _context.Compras.Add(nuevaCompra);
                await _context.SaveChangesAsync(); // Genera el IDCompras

                // 4. Crear la Línea de Compra vinculada a la Compra anterior
                var linea = new LineaDeCompra
                {
                    Idcompras = nuevaCompra.Idcompras,
                    Idproducto = prod.Idproducto,
                    Cantidad = datos.cantidad
                };
                _context.LineaDeCompras.Add(linea);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, idCompra = nuevaCompra.Idcompras });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCompra([FromBody] CompraDto datos)
        {
            try
            {
                var compra = await _context.Compras
                    .Include(c => c.LineaDeCompras)
                    .FirstOrDefaultAsync(c => c.Idcompras == datos.idCompra);

                if (compra == null) return Json(new { success = false, message = "Compra no encontrada" });

                compra.MontoCompra = datos.cantidad * datos.precio;
                compra.EstadoDePago = datos.estado;

                // Actualizar la primera línea de la compra (asumiendo venta simple)
                var linea = compra.LineaDeCompras.FirstOrDefault();
                if (linea != null)
                {
                    linea.Cantidad = datos.cantidad;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCompra(int id)
        {
            try
            {
                var compra = await _context.Compras.FindAsync(id);
                if (compra != null)
                {
                    _context.Compras.Remove(compra);
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cuenta(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Compras)
                    .ThenInclude(comp => comp.LineaDeCompras)
                        .ThenInclude(l => l.IdproductoNavigation)
                .FirstOrDefaultAsync(c => c.Idcliente == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            // Pasamos las compras ordenadas para la vista
            ViewBag.ComprasOrdenadas = cliente.Compras.OrderByDescending(c => c.FechaCompra).ToList();

            return View(cliente);
        }
    }
}