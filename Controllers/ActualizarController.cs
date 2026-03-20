using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services; // ← Necesario para EncryptionService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace FarmaciaSantaRita.Controllers
{
    //[Authorize]
    public class ActualizarController : Controller
    {
        private readonly FarmaciabdContext _context;
        private readonly EncryptionService _encryptionService;

        public ActualizarController(FarmaciabdContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        // GET: Muestra el formulario de edición
        [HttpGet]
        public IActionResult ActualizarCuenta(int idProveedor, string vista)
        {
            Console.WriteLine("[LOG] 1 - Entrando a ActualizarCuenta GET");

            try
            {
                var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"[LOG] 2 - Claim NameIdentifier: '{idUsuarioClaim ?? "NULL"}'");

                if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
                {
                    Console.WriteLine("[LOG] 3 - Fallo en parseo o claim vacío → redirigiendo a Login");
                    return RedirectToAction("Index", "Login");
                }

                Console.WriteLine($"[LOG] 4 - ID parseado: {idUsuario}");

                var usuario = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idUsuario);
                Console.WriteLine($"[LOG] 5 - Usuario encontrado: {(usuario != null ? "Sí" : "No")}");

                if (usuario == null)
                {
                    Console.WriteLine("[LOG] 6 - Usuario null → redirigiendo a Login");
                    return RedirectToAction("Index", "Login");
                }

                // Carga segura de usuarios (con try-catch)
                try
                {
                    ViewBag.Usuarios = _context.Usuarios
                        .Select(u => new
                        {
                            u.Idusuario,
                            u.Nombre,
                            u.Dni,
                            u.Apellido,
                            u.NombreUsuario,
                            u.Rol
                        })
                        .ToList();
                    Console.WriteLine("[LOG] 7 - ViewBag.Usuarios cargado OK");
                }
                catch (Exception exDb)
                {
                    Console.WriteLine($"[ERROR DB] Fallo al cargar usuarios: {exDb.Message}");
                    ViewBag.Usuarios = new List<object>(); // Lista vacía para no romper
                }

                ViewData["IdProveedor"] = idProveedor;
                ViewData["vista"] = vista;

                // Desencriptado seguro (con fallback)
                try
                {
                    ViewBag.ContraseñaActualDesencriptada = _encryptionService.Decrypt(usuario.Contraseña ?? "");
                    Console.WriteLine("[LOG] 8 - Contraseña desencriptada OK");
                }
                catch (Exception exDecrypt)
                {
                    Console.WriteLine($"[ERROR] Fallo al desencriptar: {exDecrypt.Message}");
                    ViewBag.ContraseñaActualDesencriptada = "[No disponible]";
                }

                ViewBag.FechaNacimientoFormatted = usuario.FechaNacimiento != default
                    ? usuario.FechaNacimiento.ToString("yyyy-MM-dd")
                    : "";

                ViewBag.FechaIngresoFormatted = usuario.FechaIngreso.HasValue && usuario.FechaIngreso.Value != default
                    ? usuario.FechaIngreso.Value.ToString("yyyy-MM-dd")
                    : "";

                Console.WriteLine("[LOG] 9 - Retornando View OK");
                return View(usuario);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] Excepción general en GET: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, "Error interno del servidor. Contacta al administrador.");
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCuenta(Usuario modeloActualizado, int idProveedor, string vista)
        {
            ViewData["IdProveedor"] = idProveedor;
            ViewData["vista"] = vista;

            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioClaim, out int idUsuarioAutenticado))
            {
                TempData["ResultadoActualizacion"] = "Error";
                TempData["MensajeError"] = "Sesión inválida. Inicie sesión nuevamente.";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }

            if (modeloActualizado.Idusuario != idUsuarioAutenticado)
            {
                TempData["ResultadoActualizacion"] = "Error";
                TempData["MensajeError"] = "ID de usuario inválido.";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }

            try
            {
                var usuarioParaActualizar = _context.Usuarios.Find(idUsuarioAutenticado);
                if (usuarioParaActualizar == null)
                {
                    TempData["ResultadoActualizacion"] = "Error";
                    TempData["MensajeError"] = "Usuario no encontrado.";
                    return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
                }

                // Actualizar campos editables
                usuarioParaActualizar.Nombre = modeloActualizado.Nombre?.Trim();
                usuarioParaActualizar.Apellido = modeloActualizado.Apellido?.Trim();
                usuarioParaActualizar.NombreUsuario = modeloActualizado.NombreUsuario?.Trim();
                usuarioParaActualizar.Telefono = modeloActualizado.Telefono?.Trim();
                usuarioParaActualizar.CorreoUsuario = modeloActualizado.CorreoUsuario?.Trim();
                usuarioParaActualizar.Dni = modeloActualizado.Dni?.Trim();
                usuarioParaActualizar.Direccion = modeloActualizado.Direccion?.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Rol) && modeloActualizado.Rol != usuarioParaActualizar.Rol)
                {
                    usuarioParaActualizar.Rol = modeloActualizado.Rol;
                }

                if (!string.IsNullOrWhiteSpace(modeloActualizado.NuevaContraseña))
                {
                    usuarioParaActualizar.Contraseña = _encryptionService.Encrypt(modeloActualizado.NuevaContraseña.Trim());
                }

                // Convertir fechas a UTC para PostgreSQL timestamptz
                if (usuarioParaActualizar.FechaNacimiento.Kind != DateTimeKind.Utc)
                {
                    usuarioParaActualizar.FechaNacimiento = DateTime.SpecifyKind(usuarioParaActualizar.FechaNacimiento, DateTimeKind.Utc);
                }
                if (usuarioParaActualizar.FechaIngreso.HasValue && usuarioParaActualizar.FechaIngreso.Value.Kind != DateTimeKind.Utc)
                {
                    usuarioParaActualizar.FechaIngreso = DateTime.SpecifyKind(usuarioParaActualizar.FechaIngreso.Value, DateTimeKind.Utc);
                }

                _context.Entry(usuarioParaActualizar).State = EntityState.Modified;
                int cambios = _context.SaveChanges();

                if (cambios > 0)
                {
                    TempData["ResultadoActualizacion"] = "Exito";
                    TempData["MensajeExito"] = "Los cambios se guardaron correctamente.";
                }
                else
                {
                    TempData["ResultadoActualizacion"] = "SinCambios";
                    TempData["MensajeSinCambios"] = "No se detectaron cambios para guardar.";
                }

                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
            catch (DbUpdateException dbEx)
            {
                TempData["ResultadoActualizacion"] = "Error";
                TempData["MensajeError"] = "Error en la base de datos: " + (dbEx.InnerException?.Message ?? dbEx.Message);
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
            catch (Exception ex)
            {
                TempData["ResultadoActualizacion"] = "Error";
                TempData["MensajeError"] = "Error inesperado: " + ex.Message;
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
        }





        [HttpPost]
        public IActionResult ActualizarRol([FromBody] ActualizarRolModel model)
        {
            Console.WriteLine("=== ActualizarRol INICIO ===");
            Console.WriteLine($"ID recibido: {model?.IdUsuario} | Rol nuevo: '{model?.NuevoRol}'");

            if (model == null || model.IdUsuario <= 0 || string.IsNullOrWhiteSpace(model.NuevoRol))
            {
                return Json(new { success = false, message = "Datos inválidos" });
            }

            var usuario = _context.Usuarios.Find(model.IdUsuario);

            if (usuario == null)
            {
                Console.WriteLine($"[ERROR] Usuario ID {model.IdUsuario} NO ENCONTRADO");
                return Json(new { success = false, message = "Usuario no encontrado (ver mapeo de IDUsuario)" });
            }

            Console.WriteLine($"Rol actual en BD: '{usuario.Rol ?? "(null)"}'");

            usuario.Rol = model.NuevoRol.Trim();

            _context.Entry(usuario).Property(u => u.Rol).IsModified = true;

            int cambios = _context.SaveChanges();

            Console.WriteLine($"SaveChanges afectó {cambios} filas");

            if (cambios > 0)
            {
                return Json(new { success = true, message = "Rol actualizado correctamente" });
            }

            return Json(new { success = false, message = "No se guardó (valor ya era el mismo)" });
        }

        // Clase auxiliar simple para recibir el JSON
        public class ActualizarRolModel
        {
            public int IdUsuario { get; set; }
            public string NuevoRol { get; set; }
        }





        // Acción para Gestionar Empleados (la tienes, la dejo igual)
        [HttpGet]
        public IActionResult GestionarEmpleados(int idProveedor)
        {
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                return RedirectToAction("Index", "Login");
            }

            var usuarioActual = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idUsuario);
            if (usuarioActual == null || usuarioActual.Rol != "Jefe/a")
            {
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista = "ProveedorSecundario" });
            }

            return View("CuentaEmpleado");
        }
    }
}