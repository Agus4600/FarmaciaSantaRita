using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSantaRita.Controllers
{
    
    public class RecuperarClienteController : Controller
    {
        [HttpGet]
        public IActionResult RecuperarCliente()
        {
            ViewData["Title"] = "Recuperar Cliente";
            return View("RecuperarCliente");  // Nombre de la vista sin .cshtml
        }
    }
}
