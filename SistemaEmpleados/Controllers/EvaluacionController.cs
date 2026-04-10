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
public class EvaluacionController : Controller
{
    private readonly IEvaluacionService _service;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EvaluacionController(
        IEvaluacionService service,
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

    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        EvaluacionViewModel vm;
        if (id.HasValue)
        {
            var found = await _service.GetByIdAsync(id.Value);
            if (found == null) return NotFound();
            vm = found;
        }
        else
        {
            vm = new EvaluacionViewModel();
        }

        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetKPIsEmpleado(int empleadoId)
    {
        var kpis = await _service.GetKPIsParaEmpleadoAsync(empleadoId);
        return Json(ApiResponse<object>.Ok(kpis));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] EvaluacionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var user = await _userManager.GetUserAsync(User);
        var (success, message, newId) = await _service.CreateAsync(vm, user?.Id ?? "");
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] EvaluacionViewModel vm)
    {
        var (success, message) = await _service.UpdateAsync(id, vm);
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

    // ── KPIs ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetKPIs()
    {
        var kpis = await _service.GetKPIsAsync();
        return Json(ApiResponse<object>.Ok(kpis));
    }

    [HttpGet]
    public async Task<IActionResult> FormKPI(int? id)
    {
        KPIViewModel vm;
        if (id.HasValue)
        {
            var kpi = await _context.KPIs.FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);
            if (kpi == null) return NotFound();
            vm = new KPIViewModel
            {
                Id = kpi.Id,
                Nombre = kpi.Nombre,
                Descripcion = kpi.Descripcion,
                Peso = kpi.Peso,
                PuestoId = kpi.PuestoId,
                Activo = kpi.Activo
            };
        }
        else
        {
            vm = new KPIViewModel { Activo = true };
        }

        ViewBag.Puestos = await _context.Puestos
            .Where(p => p.Activo && !p.IsDeleted)
            .OrderBy(p => p.Nombre)
            .Select(p => new { p.Id, p.Nombre })
            .ToListAsync();

        return PartialView("_FormKPI", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CreateKPI([FromBody] KPIViewModel vm)
    {
        var (success, message, newId) = await _service.CreateKPIAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EditKPI(int id, [FromBody] KPIViewModel vm)
    {
        var (success, message) = await _service.UpdateKPIAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> DeleteKPI(int id)
    {
        var (success, message) = await _service.DeleteKPIAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    private async Task CargarSelectLists()
    {
        ViewBag.Empleados = await _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .OrderBy(e => e.PrimerApellido)
            .Select(e => new { e.Id, e.PuestoId, Nombre = $"{e.PrimerNombre} {e.PrimerApellido}" })
            .ToListAsync();
    }
}
