using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services; // ← Necesario para EncryptionService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

            // Desencriptamos la contraseña actual para mostrarla en la vista
            ViewBag.ContraseñaActualDesencriptada = _encryptionService.Decrypt(usuario.Contraseña);

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
                return RedirectToAction("Index", "Login");
            }

            if (modeloActualizado.Idusuario != idUsuarioAutenticado)
            {
                TempData["ResultadoActualizacion"] = "Error: ID de usuario inválido.";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }

            try
            {
                var usuarioParaActualizar = _context.Usuarios.Find(idUsuarioAutenticado);
                if (usuarioParaActualizar == null)
                {
                    TempData["ResultadoActualizacion"] = "Error: Usuario no encontrado.";
                    return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
                }

                // Validación de campos obligatorios
                if (string.IsNullOrWhiteSpace(modeloActualizado.Nombre) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.Apellido) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.NombreUsuario) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.CorreoUsuario) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.Dni) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.Direccion) ||
                    string.IsNullOrWhiteSpace(modeloActualizado.Telefono) ||
                    modeloActualizado.FechaNacimiento == default(DateTime))
                {
                    TempData["ResultadoActualizacion"] = "Error";
                    ViewBag.ErrorMessage = "Todos los campos obligatorios deben estar completos.";
                    ViewBag.ContraseñaActualDesencriptada = _encryptionService.Decrypt(usuarioParaActualizar.Contraseña);
                    return View(usuarioParaActualizar);
                }

                // --- INICIO DE ACTUALIZACIÓN ---

                // 1. Asignamos la fecha y forzamos la marca de modificación para Entity Framework
                usuarioParaActualizar.FechaNacimiento = modeloActualizado.FechaNacimiento;
                _context.Entry(usuarioParaActualizar).Property(u => u.FechaNacimiento).IsModified = true;

                // 2. Actualizamos el resto de los campos
                usuarioParaActualizar.Nombre = modeloActualizado.Nombre.Trim();
                usuarioParaActualizar.Apellido = modeloActualizado.Apellido.Trim();
                usuarioParaActualizar.NombreUsuario = modeloActualizado.NombreUsuario.Trim();
                usuarioParaActualizar.Telefono = modeloActualizado.Telefono.Trim();
                usuarioParaActualizar.CorreoUsuario = modeloActualizado.CorreoUsuario.Trim();
                usuarioParaActualizar.Dni = modeloActualizado.Dni.Trim();
                usuarioParaActualizar.Direccion = modeloActualizado.Direccion.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Rol) && modeloActualizado.Rol != usuarioParaActualizar.Rol)
                {
                    usuarioParaActualizar.Rol = modeloActualizado.Rol;
                }

                if (!string.IsNullOrWhiteSpace(modeloActualizado.NuevaContraseña))
                {
                    usuarioParaActualizar.Contraseña = _encryptionService.Encrypt(modeloActualizado.NuevaContraseña.Trim());
                }

                // 3. Guardamos los cambios en la base de datos
                _context.SaveChanges();

                TempData["ResultadoActualizacion"] = "Exito";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
            catch (Exception ex)
            {
                // ESTO ES LO QUE FALTABA: Cerrar el bloque try y manejar la excepción
                TempData["ResultadoActualizacion"] = "Error";
                ViewBag.ErrorMessage = "Error al guardar cambios: " + ex.Message;
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