using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSantaRita.Controllers
{
    [Authorize]
    public class RecuperarClienteController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Recuperar Cliente";
            return View();
        }
    }
}
