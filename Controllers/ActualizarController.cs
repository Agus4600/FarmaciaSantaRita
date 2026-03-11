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
    [Authorize]
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
            Console.WriteLine("Entrando a ActualizarCuenta GET");

            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"Claim NameIdentifier: {idUsuarioClaim}");

            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                Console.WriteLine("Fallo en TryParse del ID usuario");
                return RedirectToAction("Index", "Login");
            }

            Console.WriteLine($"ID Usuario parseado: {idUsuario}");

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idUsuario);
            Console.WriteLine($"Usuario encontrado: {(usuario != null ? "Sí" : "No")}");

            if (usuario == null)
            {
                Console.WriteLine("Usuario no encontrado - redirigiendo a Login");
                return RedirectToAction("Index", "Login");
            }

            // Carga usuarios para offcanvas
            ViewBag.Usuarios = _context.Usuarios
                .Select(u => new { u.Idusuario, u.Nombre, u.Apellido, u.NombreUsuario, u.Rol })
                .ToList();

            ViewData["IdProveedor"] = idProveedor;
            ViewData["vista"] = vista;

            try
            {
                ViewBag.ContraseñaActualDesencriptada = _encryptionService.Decrypt(usuario.Contraseña);
                Console.WriteLine("Contraseña desencriptada OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al desencriptar contraseña: {ex.Message}");
                ViewBag.ContraseñaActualDesencriptada = "Error al desencriptar";
            }

            ViewBag.FechaNacimientoFormatted = usuario.FechaNacimiento != default
                ? usuario.FechaNacimiento.ToString("yyyy-MM-dd")
                : "";

            ViewBag.FechaIngresoFormatted = usuario.FechaIngreso.HasValue && usuario.FechaIngreso.Value != default
                ? usuario.FechaIngreso.Value.ToString("yyyy-MM-dd")
                : "";

            Console.WriteLine("Retornando View");
            return View(usuario);
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

                bool huboCambios = false;

                // Comparar cada campo antes de asignar
                if (usuarioParaActualizar.Nombre?.Trim() != modeloActualizado.Nombre?.Trim())
                {
                    usuarioParaActualizar.Nombre = modeloActualizado.Nombre?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.Apellido?.Trim() != modeloActualizado.Apellido?.Trim())
                {
                    usuarioParaActualizar.Apellido = modeloActualizado.Apellido?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.NombreUsuario?.Trim() != modeloActualizado.NombreUsuario?.Trim())
                {
                    usuarioParaActualizar.NombreUsuario = modeloActualizado.NombreUsuario?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.Telefono?.Trim() != modeloActualizado.Telefono?.Trim())
                {
                    usuarioParaActualizar.Telefono = modeloActualizado.Telefono?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.CorreoUsuario?.Trim() != modeloActualizado.CorreoUsuario?.Trim())
                {
                    usuarioParaActualizar.CorreoUsuario = modeloActualizado.CorreoUsuario?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.Dni?.Trim() != modeloActualizado.Dni?.Trim())
                {
                    usuarioParaActualizar.Dni = modeloActualizado.Dni?.Trim();
                    huboCambios = true;
                }
                if (usuarioParaActualizar.Direccion?.Trim() != modeloActualizado.Direccion?.Trim())
                {
                    usuarioParaActualizar.Direccion = modeloActualizado.Direccion?.Trim();
                    huboCambios = true;
                }
                if (!string.IsNullOrWhiteSpace(modeloActualizado.NuevaContraseña))
                {
                    usuarioParaActualizar.Contraseña = _encryptionService.Encrypt(modeloActualizado.NuevaContraseña.Trim());
                    huboCambios = true;
                }

                if (huboCambios)
                {
                    if (usuarioParaActualizar.FechaNacimiento != default(DateTime))
                    {
                        usuarioParaActualizar.FechaNacimiento = DateTime.SpecifyKind(usuarioParaActualizar.FechaNacimiento, DateTimeKind.Utc);
                    }
                    if (usuarioParaActualizar.FechaIngreso.HasValue && usuarioParaActualizar.FechaIngreso.Value != default(DateTime))
                    {
                        usuarioParaActualizar.FechaIngreso = DateTime.SpecifyKind(usuarioParaActualizar.FechaIngreso.Value, DateTimeKind.Utc);
                    }
                    _context.Entry(usuarioParaActualizar).State = EntityState.Modified;
                    int cambios = _context.SaveChanges();

                    TempData["ResultadoActualizacion"] = "Exito";
                    TempData["MensajeExito"] = "Los cambios se guardaron correctamente.";
                }
                else
                {
                    TempData["ResultadoActualizacion"] = "SinCambios";
                    TempData["MensajeSinCambios"] = "No se detectaron cambios para guardar.";
                }

                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }  // ← Esta llave cierra el try (la que faltaba)
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
        [ValidateAntiForgeryToken]
        public JsonResult ActualizarRol(int idUsuario, string nuevoRol)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario == null) return Json(new { success = false, message = "Usuario no encontrado" });

            usuario.Rol = nuevoRol;
            _context.SaveChanges();

            return Json(new { success = true });
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