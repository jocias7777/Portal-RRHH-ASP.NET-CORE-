using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class AsistenciaController : Controller
{
    private readonly IAsistenciaService _service;
    private readonly ApplicationDbContext _context;

    public AsistenciaController(IAsistenciaService service, ApplicationDbContext context)
    {
        _service = service;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        await CargarSelectLists();
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
    public async Task<IActionResult> GetById(int id)
    {
        var vm = await _service.GetByIdAsync(id);
        if (vm == null) return NotFound();
        return Json(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        var vm = id.HasValue
            ? await _service.GetByIdAsync(id.Value)
            : new AsistenciaViewModel { Fecha = DateTime.Today };

        if (vm == null) return NotFound();
        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] AsistenciaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message, newId) = await _service.CreateAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] AsistenciaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

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

    [HttpGet]
    public async Task<IActionResult> GetKpisHoy()
    {
        var kpis = await _service.GetKpisHoyAsync();
        return Json(kpis);
    }

    [HttpGet]
    public async Task<IActionResult> GetHorariosActivos()
    {
        var horarios = await _context.Horarios
            .Where(h => !h.IsDeleted && h.Activo)
            .OrderBy(h => h.Nombre)
            .Select(h => new
            {
                h.Id,
                h.Nombre,
                HoraEntrada = h.HoraEntrada.ToString(@"hh\:mm"),
                HoraSalida = h.HoraSalida.ToString(@"hh\:mm"),
                h.MinutosToleranciaTardanza
            })
            .ToListAsync();

        return Json(horarios);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> RegistrarSalida(int id, [FromBody] RegistrarSalidaDto dto)
    {
        var registro = await _context.Asistencias.FindAsync(id);
        if (registro == null) return Json(ApiResponse.Fail("Registro no encontrado."));

        registro.HoraSalida = dto.HoraSalida;
        await _context.SaveChangesAsync();

        return Json(ApiResponse.Ok("Salida registrada correctamente."));
    }

    public class RegistrarSalidaDto
    {
        public TimeSpan? HoraSalida { get; set; }
    }
    private async Task CargarSelectLists()
    {
        ViewBag.Empleados = await _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == SistemaEmpleados.Models.Entities.EstadoEmpleado.Activo)
            .OrderBy(e => e.PrimerApellido)
            .Select(e => new { e.Id, Nombre = $"{e.PrimerNombre} {e.PrimerApellido}" })
            .ToListAsync();

        // ✨ ACTUALIZADO: incluye HoraEntrada, HoraSalida y MinutosToleranciaTardanza
        ViewBag.Horarios = await _context.Horarios
            .Where(h => !h.IsDeleted && h.Activo)
            .OrderBy(h => h.Nombre)
            .Select(h => new
            {
                h.Id,
                h.Nombre,
                HoraEntrada = h.HoraEntrada.ToString(@"hh\:mm"),
                HoraSalida = h.HoraSalida.ToString(@"hh\:mm"),
                h.MinutosToleranciaTardanza
            })
            .ToListAsync();

        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => !d.IsDeleted && d.Activo)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
    }
}