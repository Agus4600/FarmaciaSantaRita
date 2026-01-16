using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace FarmaciaSantaRita.Controllers
{
    // El atributo [Authorize] protege todas las acciones
    [Authorize]
    public class BoletaController : Controller
    {
        private readonly FarmaciabdContext _context;

        public BoletaController(FarmaciabdContext context)
        {
            _context = context;
        }

        // ************************************************
        // * ACCIÓN PRINCIPAL (Index) - Carga la Vista
        // ************************************************
        [HttpGet]
        public IActionResult Index(int idProveedor)
        {
            // 1. Buscar el proveedor por su ID para establecer el contexto
            var proveedor = _context.Proveedors.FirstOrDefault(p => p.Idproveedor == idProveedor);

            if (proveedor == null)
            {
                return NotFound($"Error: Proveedor con ID {idProveedor} no encontrado.");
            }

            // 2. Usar ViewBag para pasar el ID y nombre a la vista.
            ViewBag.IdProveedor = idProveedor;
            ViewBag.NombreProveedor = proveedor.NombreProveedor;

            // 3. Devuelve la vista /Views/Boleta/Index.cshtml (La lista de boletas se cargará
            //    por una llamada AJAX separada a GetBoletas, una vez que la vista esté lista).
            return View();
        }

        // ************************************************
        // * NUEVA ACCIÓN: Obtiene la lista de Boletas (JSON)
        // ************************************************
        [HttpGet]
        public IActionResult GetBoletas(int idProveedor)
        {
            // Asegurarse de que el ID del proveedor es válido
            if (idProveedor <= 0)
            {
                return BadRequest(new { success = false, message = "ID de proveedor inválido." });
            }

            try
            {
                // Obtener solo las boletas del proveedor específico
                var boletas = _context.Boleta
                    .Where(b => b.Idproveedor == idProveedor)
                    // Seleccionar solo las propiedades necesarias y darles formato
                    .Select(b => new
                    {
                        Idboleta = b.Idboleta,
                        // Formateamos la fecha para la vista
                        Fecha = b.Fecha.ToString("dd/MM/yyyy"),
                        ImporteFinal = b.ImporteFinal,
                        Transfer = b.Transfer,
                        Categoria = b.Categoria,
                        Detalle = b.Detalle
                    })
                    .OrderByDescending(b => b.Fecha) // Ordenar por fecha para mejor visualización
                    .ToList();

                // Devolver la lista de boletas en formato JSON
                return Json(new { success = true, data = boletas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener boletas: {ex.Message}" });
            }
        }


        // DTO para recibir desde AJAX (Fecha como string) - opcional pero útil
        public class BoletaDto
        {
            public int Idusuario { get; set; }
            public int Idproveedor { get; set; }
            public string Fecha { get; set; } = string.Empty; // "yyyy-MM-dd"
            public int ImporteFinal { get; set; }
            public string Transfer { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string? Detalle { get; set; }
        }

        [HttpDelete]
        public async Task<IActionResult> EliminarBoleta(int id)
        {
            try
            {
                var boleta = await _context.Boleta.FindAsync(id);

                if (boleta == null)
                    return Json(new { success = false, message = "La boleta no existe." });

                _context.Boleta.Remove(boleta);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // ------------------ CargarExcel: lee Fecha / Importe / Transfer y añade Categoria según el nombre de la hoja ------------------
        [HttpPost]
        public IActionResult CargarExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se seleccionó ningún archivo.");

            try
            {
                var categorias = new List<object>();

                using (var stream = new MemoryStream())
                {
                    archivo.CopyTo(stream);
                    using var package = new ExcelPackage(stream);
                    foreach (var hoja in package.Workbook.Worksheets)
                    {
                        string categoria = hoja.Name;
                        var registros = new List<object>();

                        int filaInicio = 3; // empezamos en fila 3
                        int ultimaFila = hoja.Dimension?.End.Row ?? 0;

                        for (int fila = filaInicio; fila <= ultimaFila; fila++)
                        {
                            string fecha = hoja.Cells[fila, 1].Text;
                            string importe = hoja.Cells[fila, 2].Text;
                            string transfer = hoja.Cells[fila, 3].Text;

                            if (string.IsNullOrWhiteSpace(fecha) &&
                                string.IsNullOrWhiteSpace(importe) &&
                                string.IsNullOrWhiteSpace(transfer))
                                continue;

                            registros.Add(new
                            {
                                Fecha = fecha,
                                Importe = importe,
                                Transfer = transfer
                            });
                        }

                        categorias.Add(new
                        {
                            Categoria = categoria,
                            Datos = registros
                        });
                    }
                }

                return Json(categorias);
            }
            catch (Exception ex)
            {
                return BadRequest("Error leyendo Excel: " + ex.Message);
            }
        }

        // ************************************************
        // * ACCIÓN MODIFICADA: Guardar Boleta (usa DateTime)
        // ************************************************
        [HttpPost]
        public async Task<IActionResult> GuardarBoleta([FromBody] BoletaDto data)
        {
            try
            {
                // 1. Convertir la fecha a UTC (esto soluciona el error)
                if (!DateTime.TryParseExact(data.Fecha, "yyyy-MM-dd",
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            System.Globalization.DateTimeStyles.None,
                                            out DateTime fechaParsed))
                {
                    return BadRequest(new { exito = false, mensaje = "Formato de fecha inválido. Se espera yyyy-MM-dd." });
                }

                // ★★★ Línea clave: Forzar UTC ★★★
                fechaParsed = DateTime.SpecifyKind(fechaParsed, DateTimeKind.Utc);
                // O mejor aún (si la fecha viene sin hora): 
                // fechaParsed = fechaParsed.ToUniversalTime();

                var boleta = new Boletum
                {
                    Idusuario = data.Idusuario,
                    Idproveedor = data.Idproveedor,
                    Fecha = fechaParsed,  // Ahora es UTC → PostgreSQL lo acepta
                    ImporteFinal = data.ImporteFinal,
                    Transfer = data.Transfer,
                    Categoria = data.Categoria,
                    Detalle = data.Detalle ?? string.Empty
                };

                _context.Boleta.Add(boleta);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    exito = true,
                    idBoleta = boleta.Idboleta
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine("❌ ERROR al guardar boleta: " + inner);
                return Json(new
                {
                    exito = false,
                    mensaje = "Error DB: " + inner
                });
            }
        }


        // ExportarExcel (igual que antes)...
        [HttpGet]
        public IActionResult ExportarExcel(int idProveedor)
        {
            var datos = _context.Boleta.Where(b => b.Idproveedor == idProveedor).ToList();

            if (!datos.Any())
                return BadRequest("No hay boletas para exportar.");

            var proveedor = _context.Proveedors.FirstOrDefault(p => p.Idproveedor == idProveedor);
            string nombreProveedor = proveedor?.NombreProveedor ?? "Proveedor";
            string fechaActual = DateTime.Now.ToString("dd-MM-yyyy HH-mm");
            string nombreArchivo = $"{nombreProveedor} {fechaActual}.xlsx";

            ExcelPackage.License.SetNonCommercialOrganization("Drogueria Suiza");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Boletas");

            ws.Cells["A1"].Value = "Fecha";
            ws.Cells["B1"].Value = "Importe";
            ws.Cells["C1"].Value = "Transfer";
            ws.Cells["A1:C1"].Style.Font.Bold = true;

            int row = 2;
            int conTransfer = 0;
            int sinTransfer = 0;

            foreach (var b in datos)
            {
                ws.Cells[row, 1].Value = b.Fecha.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = b.ImporteFinal;
                ws.Cells[row, 3].Value = b.Transfer;

                if (b.Transfer?.ToUpper() == "SI")
                    conTransfer++;
                else
                    sinTransfer++;

                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            ws.Cells["E2"].Value = "Resumen Transfer";
            ws.Cells["E2"].Style.Font.Bold = true;
            ws.Cells["E3"].Value = "Con Transfer (SI):";
            ws.Cells["F3"].Value = conTransfer;
            ws.Cells["E4"].Value = "Sin Transfer (NO):";
            ws.Cells["F4"].Value = sinTransfer;

            var bytes = package.GetAsByteArray();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }
    }
}