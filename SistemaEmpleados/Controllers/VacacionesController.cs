using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class VacacionesController : Controller
{
    private readonly IVacacionService _service;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VacacionesController(
        IVacacionService service,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView() : View();
    }

    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] DataTablesRequest request)
    {
        var result = await _service.GetDataTablesAsync(request);
        return Json(result);
    }

    // ── Formulario solicitud ──
    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        var vm = id.HasValue
            ? await _service.GetByIdAsync(id.Value)
            : new VacacionViewModel();

        if (vm == null) return NotFound();
        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] VacacionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var user = await _userManager.GetUserAsync(User);
        var (success, message, newId) = await _service.CreateAsync(vm, user?.NombreCompleto ?? "");
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] VacacionViewModel vm)
    {
        var (success, message) = await _service.UpdateAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Aprobar(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var (success, message) = await _service.AprobarAsync(id, user?.NombreCompleto ?? "");
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] string motivo)
    {
        var (success, message) = await _service.RechazarAsync(id, motivo);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpGet]
    public async Task<IActionResult> DiasDisponibles(int empleadoId)
    {
        var dias = await _service.GetDiasDisponiblesAsync(empleadoId, DateTime.Now.Year);
        return Json(ApiResponse<object>.Ok(new { dias }));
    }

    // ── KPIs ──
    [HttpGet]
    public async Task<IActionResult> GetKPIs()
    {
        var kpis = await _service.GetKPIsAsync();
        return Json(ApiResponse<VacacionKpiViewModel>.Ok(kpis));
    }

    // ── Ausencias ──
    [HttpGet]
    public async Task<IActionResult> GetAusencias()
    {
        var data = await _service.GetAusenciasAsync();
        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> FormAusencia()
    {
        await CargarSelectLists();
        return PartialView("_FormAusencia", new AusenciaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CreateAusencia([FromBody] AusenciaViewModel vm)
    {
        var (success, message) = await _service.CreateAusenciaAsync(vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> DeleteAusencia(int id)
    {
        var (success, message) = await _service.DeleteAusenciaAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── Saldos ──
    [HttpGet]
    public async Task<IActionResult> GetSaldos()
    {
        var data = await _service.GetSaldosAsync();
        return Json(new { success = true, data });
    }

    private async Task CargarSelectLists()
    {
        ViewBag.Empleados = await _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .OrderBy(e => e.PrimerApellido)
            .Select(e => new { e.Id, Nombre = $"{e.PrimerNombre} {e.PrimerApellido}" })
            .ToListAsync();
    }
}