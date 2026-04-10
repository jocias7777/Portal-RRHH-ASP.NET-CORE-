using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class PersonalController : Controller
{
    private readonly IEmpleadoService _service;
    private readonly ApplicationDbContext _context;

    public PersonalController(IEmpleadoService service, ApplicationDbContext context)
    {
        _service = service;
        _context = context;
    }

    // ── GET: /Personal
    public IActionResult Index()
    {
        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView("_IndexPartial") : View();
    }

    // ── GET: /Personal/GetAll?departamentoId=&estado=
    [HttpGet]
    public async Task<IActionResult> GetAll(int? departamentoId, string? estado)
    {
        try
        {
            var query = _context.Empleados
                .Include(e => e.Departamento)
                .Include(e => e.Puesto)
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (departamentoId.HasValue && departamentoId > 0)
                query = query.Where(e => e.DepartamentoId == departamentoId);

            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoEmpleado>(estado, out var estadoEnum))
                query = query.Where(e => e.Estado == estadoEnum);

            var data = await query
                .OrderBy(e => e.PrimerNombre)
                .ThenBy(e => e.PrimerApellido)
                .ToListAsync();

            var result = data.Select(e => new
            {
                id = e.Id,
                codigo = e.Codigo,
                nombreCompleto = e.NombreCompleto,
                email = e.Email,
                departamento = e.Departamento?.Nombre ?? "—",
                puesto = e.Puesto?.Nombre ?? "—",
                tipoContrato = e.TipoContrato.ToString(),
                fechaIngreso = e.FechaIngreso.ToString("dd/MM/yyyy"),
                estado = e.Estado.ToString(),
                fotoUrl = e.FotoUrl,

                // ── LÍNEAS NUEVAS ──
                salarioBase = e.SalarioBase,
                salarioMinimo = e.Puesto != null ? e.Puesto.SalarioMinimo : 0m,
                salarioMaximo = e.Puesto != null ? e.Puesto.SalarioMaximo : 0m,
                puestoId = e.PuestoId,
                departamentoId = e.DepartamentoId
            });

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, data = Array.Empty<object>(), message = ex.Message });
        }
    }

    // ── POST: /Personal/GetData  (DataTables — se mantiene por compatibilidad)
    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] DataTablesRequest request)
    {
        var result = await _service.GetDataTablesAsync(request);
        return Json(result);
    }

    // ── GET: /Personal/Form?id=
    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        var vm = id.HasValue
            ? (EmpleadoViewModel?)await _service.GetByIdAsync(id.Value)
            : new EmpleadoViewModel
            {
                FechaIngreso = DateTime.Today,
                FechaNacimiento = DateTime.Today.AddYears(-25)
            };

        if (vm == null) return NotFound();

        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    // ── POST: /Personal/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] EmpleadoViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(k => k.Key, v => v.Value!.Errors.Select(e => e.ErrorMessage).ToList());
            return Json(ApiResponse.Fail("Datos inválidos.",
                errors.Values.SelectMany(x => x).ToList()));
        }

        var (success, message, newId) = await _service.CreateAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    // ── POST: /Personal/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] EmpleadoViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message) = await _service.UpdateAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── POST: /Personal/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── GET: /Personal/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _service.GetByIdAsync(id);
        if (vm == null) return NotFound();

        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView("_Detalle", vm) : View("Detalle", vm);
    }

    // ── GET: /Personal/Search?q=
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { groups = Array.Empty<object>() });

        var results = await _service.SearchForGlobalAsync(q);
        return Json(new
        {
            groups = new[] { new { modulo = "Personal", items = results } }
        });
    }

    // ── Helpers privados
    private async Task CargarSelectLists()
    {
        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();

        ViewBag.Puestos = await _context.Puestos
            .Where(p => p.Activo && !p.IsDeleted)
            .OrderBy(p => p.Nombre)
            .Select(p => new { p.Id, p.Nombre, p.DepartamentoId })
            .ToListAsync();
    }
}