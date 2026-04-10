using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // ─────────────────────────────────────────────────────────────
    // LOGIN
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var returnUrl = model.ReturnUrl;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente.");
        else
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────
    // LOGOUT
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    // ─────────────────────────────────────────────────────────────
    // PERFIL
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        return View(user);
    }

    // ─────────────────────────────────────────────────────────────
    // CAMBIAR CONTRASEÑA
    // ─────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(
        string PasswordActual,
        string NuevoPassword,
        string ConfirmarPassword)
    {
        if (NuevoPassword != ConfirmarPassword)
        {
            TempData["Error"] = "Las contraseñas nuevas no coinciden.";
            return RedirectToAction("Profile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, PasswordActual, NuevoPassword);

        if (result.Succeeded)
        {
            // Refresca la cookie para que no cierre sesión
            await _signInManager.RefreshSignInAsync(user);
            TempData["Exito"] = "Contraseña actualizada correctamente.";
        }
        else
        {
            var error = result.Errors.FirstOrDefault()?.Description
                        ?? "Error al cambiar la contraseña.";
            TempData["Error"] = error;
        }

        return RedirectToAction("Profile");
    }
}