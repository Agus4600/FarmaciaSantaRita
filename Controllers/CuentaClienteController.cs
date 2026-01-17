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
            var comprasBD = await _context.Compras
    .Include(c => c.IdclienteNavigation)
    .Include(c => c.IdlineaDeCompraNavigation)  // ← "l" minúscula
        .ThenInclude(l => l.IdproductoNavigation)
    .OrderByDescending(c => c.Idcompras)
    .ToListAsync();

            var viewModel = new
            {
                Clientes = await _context.Clientes
                    .Select(c => new
                    {
                        idcliente = c.Idcliente,
                        nombreCliente = c.NombreCliente,
                        telefonoCliente = c.TelefonoCliente,
                        direccionCliente = c.DireccionCliente
                    }).ToListAsync(),
                Productos = await _context.Productos.ToListAsync(),
                ComprasExistentes = comprasBD
            };

            return View(viewModel);
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNuevaCompra([FromBody] CompraDto datos)
        {
            if (datos == null) return Json(new { success = false, message = "Datos nulos" });

            try
            {
                Cliente cliente = null;

                // 1. Buscar SOLO por DNI (Identificador único)
                if (!string.IsNullOrEmpty(datos.dni))
                {
                    cliente = await _context.Clientes
                        .FirstOrDefaultAsync(c => c.DNI == datos.dni.Trim());

                    if (cliente != null)
                    {
                        // Existe por DNI → Actualizar datos con lo que viene del formulario
                        cliente.NombreCliente = datos.nombre?.Trim() ?? cliente.NombreCliente;

                        if (!string.IsNullOrEmpty(datos.telefono))
                            cliente.TelefonoCliente = datos.telefono.Trim();

                        if (!string.IsNullOrEmpty(datos.direccion))
                            cliente.DireccionCliente = datos.direccion.Trim();

                        cliente.EstadoDePago = "Pendiente";
                        _context.Clientes.Update(cliente);
                    }
                }

                // 2. Si no existe por DNI (o no se ingresó DNI) → Crear nuevo cliente
                // Eliminamos la búsqueda por nombre para evitar que personas distintas se pisen
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

                // Guardamos cambios del cliente para asegurar que tengamos su ID
                await _context.SaveChangesAsync();

                // 3. Manejo del Producto
                var nombreProdNorm = datos.producto.ToLower().Trim();
                var prod = await _context.Productos
                    .FirstOrDefaultAsync(p => p.NombreProducto.ToLower() == nombreProdNorm);

                if (prod == null)
                {
                    prod = new Producto
                    {
                        NombreProducto = datos.producto.Trim(),
                        PrecioUnitario = datos.precio
                    };
                    _context.Productos.Add(prod);
                }
                else
                {
                    prod.PrecioUnitario = datos.precio;
                    _context.Productos.Update(prod);
                }
                await _context.SaveChangesAsync();

                // 4. Crear Línea de Compra
                var linea = new LineaDeCompra { Idproducto = prod.Idproducto, Cantidad = datos.cantidad };
                _context.Set<LineaDeCompra>().Add(linea);
                await _context.SaveChangesAsync();

                // 5. ID usuario logueado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int idUsuarioLogueado = string.IsNullOrEmpty(userIdClaim) ? 1 : int.Parse(userIdClaim);

                // 6. Fecha segura - Parseo inteligente
                DateOnly fechaCompra;
                if (!string.IsNullOrWhiteSpace(datos.fecha) && DateOnly.TryParse(datos.fecha, out DateOnly parsedFecha))
                {
                    fechaCompra = parsedFecha;
                }
                else
                {
                    fechaCompra = DateOnly.FromDateTime(DateTime.Now);
                }

                // 7. Crear la Compra final
                int proximoIdCompra = await _context.Compras.AnyAsync() ? await _context.Compras.MaxAsync(c => c.Idcompras) + 1 : 1;
                var nuevaCompra = new Compra
                {
                    Idcompras = proximoIdCompra,
                    IdlineaDeCompra = linea.IdlineaDeCompra,
                    Descripcion = $"Venta de {datos.producto}",
                    FechaCompra = fechaCompra,
                    MontoCompra = datos.cantidad * datos.precio,
                    Idcliente = cliente.Idcliente, // ID del cliente que encontramos o creamos arriba
                    Idusuario = idUsuarioLogueado,
                    EstadoDePago = "pendiente"
                };

                _context.Compras.Add(nuevaCompra);
                await _context.SaveChangesAsync();

                return Json(new { success = true, idCompra = nuevaCompra.Idcompras });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }






        // El resto del controlador (ActualizarCompra, EliminarCompra, Cuenta) queda igual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCompra([FromBody] CompraDto datos)
        {
            try
            {
                var compra = await _context.Compras
                    .FirstOrDefaultAsync(c => c.Idcompras == datos.idCompra);
                if (compra == null) return Json(new { success = false, message = "Compra no encontrada" });

                // Actualizar monto
                compra.MontoCompra = datos.cantidad * datos.precio;

                // Actualizar cantidad en la línea de compra
                var linea = await _context.Set<LineaDeCompra>()
    .FirstOrDefaultAsync(l => l.IdlineaDeCompra == compra.IdlineaDeCompra);  // ← "l" minúscula
                if (linea != null)
                {
                    linea.Cantidad = datos.cantidad;
                }

                // ACTUALIZAR ESTADO DE PAGO
                compra.EstadoDePago = datos.estado;

                // Marcar la entidad como modificada
                _context.Entry(compra).State = EntityState.Modified;

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
                .FirstOrDefaultAsync(c => c.Idcliente == id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            var compras = await _context.Compras
                .Where(c => c.Idcliente == id)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();

            var comprasConDetalle = new List<object>();
            foreach (var compra in compras)
            {
                var lineas = await _context.Set<LineaDeCompra>()
    .Include(l => l.IdproductoNavigation)
    .Where(l => l.IdlineaDeCompra == compra.IdlineaDeCompra)  // ← "l" minúscula
    .ToListAsync();

                comprasConDetalle.Add(new
                {
                    Compra = compra,
                    Lineas = lineas
                });
            }

            ViewBag.ComprasConDetalle = comprasConDetalle;
            return View(cliente);
        }
    }
}