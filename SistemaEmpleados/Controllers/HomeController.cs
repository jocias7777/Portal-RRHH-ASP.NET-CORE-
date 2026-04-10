using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = user != null
            ? await _userManager.GetRolesAsync(user)
            : new List<string>();

        ViewBag.UserName = user?.NombreCompleto ?? User.Identity?.Name ?? "Usuario";
        ViewBag.UserRole = roles.FirstOrDefault() ?? "Sin rol";

        // ← CLAVE: le dice al layout que estamos en el dashboard
        ViewBag.IsDashboard = true;

        return View();
    }
}