using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSantaRita.Controllers
{
    public class PanelSeleccionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
