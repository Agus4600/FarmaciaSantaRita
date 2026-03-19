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
            Console.WriteLine("[LOG] ActualizarRol REAL llamado - Inicio");

            if (model == null)
            {
                Console.WriteLine("[ERROR] model es NULL");
                return Json(new { success = false, message = "Model nulo - no se recibieron datos" });
            }

            Console.WriteLine($"[LOG] IdUsuario recibido: {model.IdUsuario}");
            Console.WriteLine($"[LOG] NuevoRol recibido: '{model.NuevoRol}' (longitud: {model.NuevoRol?.Length ?? 0})");

            if (model.IdUsuario <= 0 || string.IsNullOrWhiteSpace(model.NuevoRol))
            {
                Console.WriteLine("[ERROR] Datos inválidos");
                return Json(new { success = false, message = "Datos inválidos (ID o rol vacío)" });
            }

            try
            {
                var usuario = _context.Usuarios.Find(model.IdUsuario);
                if (usuario == null)
                {
                    Console.WriteLine($"[ERROR] Usuario NO encontrado con ID: {model.IdUsuario}");
                    return Json(new { success = false, message = $"Usuario con ID {model.IdUsuario} no encontrado" });
                }

                Console.WriteLine($"[LOG] Usuario encontrado - Rol actual en BD: '{usuario.Rol ?? "null"}' (longitud: {usuario.Rol?.Length ?? 0})");

                // Log antes de cambiar
                Console.WriteLine($"[LOG] Intentando cambiar Rol: '{usuario.Rol}' → '{model.NuevoRol}'");

                // Forzamos el cambio aunque sean "iguales" (para depurar)
                usuario.Rol = model.NuevoRol.Trim();  // Quitamos posibles espacios

                // Marcamos explícitamente como modificado
                _context.Entry(usuario).Property(x => x.Rol).IsModified = true;

                // Logs de fechas (por si acaso)
                Console.WriteLine($"[LOG] FechaNacimiento Kind: {usuario.FechaNacimiento.Kind}");
                Console.WriteLine($"[LOG] FechaIngreso Kind: {(usuario.FechaIngreso.HasValue ? usuario.FechaIngreso.Value.Kind.ToString() : "null")}");

                int cambios = _context.SaveChanges();
                Console.WriteLine($"[LOG] SaveChanges retornó: {cambios} cambios");

                if (cambios > 0)
                {
                    Console.WriteLine("[LOG] Cambio guardado exitosamente");
                    return Json(new { success = true, message = "Rol actualizado correctamente" });
                }
                else
                {
                    Console.WriteLine("[LOG] NO se guardaron cambios - posible razón: valor ya era el mismo o EF no detectó modificación");
                    return Json(new { success = false, message = "No se detectaron cambios (ver logs del servidor)" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR CRÍTICO] Excepción en ActualizarRol: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error interno al guardar: " + ex.Message });
            }
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