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
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                return RedirectToAction("Index", "Login");
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idUsuario);
            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewData["IdProveedor"] = idProveedor;
            ViewData["vista"] = vista;

            // Desencriptamos la contraseña actual
            ViewBag.ContraseñaActualDesencriptada = _encryptionService.Decrypt(usuario.Contraseña);

            // Formateo de FechaNacimiento (DateTime normal, no nullable)
            ViewBag.FechaNacimientoFormatted = usuario.FechaNacimiento != default(DateTime)
                ? usuario.FechaNacimiento.ToString("yyyy-MM-dd")
                : "";

            // Formateo de FechaIngreso (DateTime?, sí nullable)
            ViewBag.FechaIngresoFormatted = usuario.FechaIngreso.HasValue && usuario.FechaIngreso.Value != default(DateTime)
                ? usuario.FechaIngreso.Value.ToString("yyyy-MM-dd")
                : "";

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
                if (usuarioParaActualizar.FechaNacimiento != default(DateTime))
                {
                    usuarioParaActualizar.FechaNacimiento = DateTime.SpecifyKind(usuarioParaActualizar.FechaNacimiento, DateTimeKind.Utc);
                }

                if (usuarioParaActualizar.FechaIngreso.HasValue)
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
                    TempData["MensajeError"] = "No se detectaron cambios para guardar.";
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