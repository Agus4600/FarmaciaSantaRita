using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSantaRita.Models;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
[Route("GestionUsuarios")]  // ← Mantiene ruta amigable
public class gestionusuarioscontroller : Controller
{
    private readonly FarmaciabdContext _context;
    public gestionusuarioscontroller(FarmaciabdContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("Index")]
    public IActionResult Index()
    {
        var usuarios = _context.Usuarios.ToList();
        return View(usuarios);
    }
}