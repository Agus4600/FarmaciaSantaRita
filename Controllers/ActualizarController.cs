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
                // Buscar el usuario en la BD (tracked por EF)
                var usuarioParaActualizar = _context.Usuarios.Find(idUsuarioAutenticado);
                if (usuarioParaActualizar == null)
                {
                    TempData["ResultadoActualizacion"] = "Error: Usuario no encontrado.";
                    return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
                }

                // Validación manual
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

                // Actualizar TODOS los campos directamente
                usuarioParaActualizar.Nombre = modeloActualizado.Nombre.Trim();
                usuarioParaActualizar.Apellido = modeloActualizado.Apellido.Trim();
                usuarioParaActualizar.NombreUsuario = modeloActualizado.NombreUsuario.Trim();
                usuarioParaActualizar.Telefono = modeloActualizado.Telefono.Trim();
                usuarioParaActualizar.CorreoUsuario = modeloActualizado.CorreoUsuario.Trim();
                usuarioParaActualizar.Dni = modeloActualizado.Dni.Trim();
                usuarioParaActualizar.Direccion = modeloActualizado.Direccion.Trim();
                usuarioParaActualizar.FechaNacimiento = modeloActualizado.FechaNacimiento;

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Rol) && modeloActualizado.Rol != usuarioParaActualizar.Rol)
                {
                    usuarioParaActualizar.Rol = modeloActualizado.Rol;
                }

                // Contraseña nueva (solo si se ingresó)
                if (!string.IsNullOrWhiteSpace(modeloActualizado.NuevaContraseña))
                {
                    usuarioParaActualizar.Contraseña = _encryptionService.Encrypt(modeloActualizado.NuevaContraseña.Trim());
                }

                // Guardar
                _context.SaveChanges();

                TempData["ResultadoActualizacion"] = "Exito";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
            catch (Exception ex)
            {
                // Loguear error para debug (puede verse en consola del servidor o logs)
                Console.WriteLine($"Error al actualizar cuenta: {ex.Message} | Inner: {ex.InnerException?.Message}");

                TempData["ResultadoActualizacion"] = "Error";
                ViewBag.ErrorMessage = "Error al guardar cambios: " + ex.Message;

                // Aquí NO usamos usuarioParaActualizar (porque puede ser null si la excepción ocurrió antes)
                // Si querés mostrar la contraseña actual, podés volver a buscarla
                var usuarioTemp = _context.Usuarios.Find(idUsuarioAutenticado);
                ViewBag.ContraseñaActualDesencriptada = usuarioTemp != null
                    ? _encryptionService.Decrypt(usuarioTemp.Contraseña)
                    : "";

                return View(modeloActualizado);  // Devolvemos el modelo enviado para que se vean los valores
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