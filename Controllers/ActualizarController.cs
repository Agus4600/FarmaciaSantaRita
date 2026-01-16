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

            // 1. Obtener ID del usuario autenticado desde el claim
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioClaim, out int idUsuarioAutenticado))
            {
                TempData["ResultadoActualizacion"] = "Error";
                return RedirectToAction("Index", "Login");
            }

            // 2. Validar que el ID enviado coincida con el autenticado
            if (modeloActualizado.Idusuario != idUsuarioAutenticado)
            {
                TempData["ResultadoActualizacion"] = "Error: ID de usuario inválido.";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }

            try
            {
                // 3. Buscar el usuario REAL en la BD usando el ID autenticado
                var usuarioParaActualizar = _context.Usuarios.Find(idUsuarioAutenticado);
                if (usuarioParaActualizar == null)
                {
                    TempData["ResultadoActualizacion"] = "Error: Usuario no encontrado.";
                    return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
                }

                // 4. Actualizar solo los campos que vinieron (evitar sobrescribir con null)
                if (!string.IsNullOrWhiteSpace(modeloActualizado.Nombre))
                    usuarioParaActualizar.Nombre = modeloActualizado.Nombre.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Apellido))
                    usuarioParaActualizar.Apellido = modeloActualizado.Apellido.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.NombreUsuario))
                    usuarioParaActualizar.NombreUsuario = modeloActualizado.NombreUsuario.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Telefono))
                    usuarioParaActualizar.Telefono = modeloActualizado.Telefono.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.CorreoUsuario))
                    usuarioParaActualizar.CorreoUsuario = modeloActualizado.CorreoUsuario.Trim();

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Dni))
                    usuarioParaActualizar.Dni = modeloActualizado.Dni.Trim();

                if (modeloActualizado.FechaNacimiento != default(DateTime))
                    usuarioParaActualizar.FechaNacimiento = modeloActualizado.FechaNacimiento;

                if (!string.IsNullOrWhiteSpace(modeloActualizado.Direccion))
                    usuarioParaActualizar.Direccion = modeloActualizado.Direccion.Trim();

                // Rol: solo si cambió (por seguridad, quizás no permitir cambiar rol aquí)
                if (!string.IsNullOrWhiteSpace(modeloActualizado.Rol) && modeloActualizado.Rol != usuarioParaActualizar.Rol)
                {
                    usuarioParaActualizar.Rol = modeloActualizado.Rol;
                }

                // Contraseña nueva (solo si se ingresó)
                if (!string.IsNullOrWhiteSpace(modeloActualizado.NuevaContraseña))
                {
                    usuarioParaActualizar.Contraseña = _encryptionService.Encrypt(modeloActualizado.NuevaContraseña.Trim());
                }

                // 5. Guardar cambios
                _context.SaveChanges();

                TempData["ResultadoActualizacion"] = "Exito";
                return RedirectToAction("ActualizarCuenta", new { idProveedor, vista });
            }
            catch (Exception ex)
            {
                TempData["ResultadoActualizacion"] = "Error";
                ViewBag.ErrorMessage = ex.Message; // Opcional: mostrar el error real en la vista
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