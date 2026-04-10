using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class HorariosController : Controller
{
    private readonly IAsistenciaService _service;

    public HorariosController(IAsistenciaService service)
    {
        _service = service;
    }

    // GET /Horarios — retorna la vista completa o partial si es AJAX
    public IActionResult Index()
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isAjax ? PartialView() : View();
    }

    // POST /Horarios/GetData — DataTables server-side
    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] DataTablesRequest request)
    {
        var result = await _service.GetHorariosDataTablesAsync(request);
        return Json(result);
    }

    // GET /Horarios/Form?id=  — carga modal crear o editar
    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        var vm = id.HasValue
            ? await _service.GetHorarioByIdAsync(id.Value)
            : new HorarioViewModel();

        if (vm == null) return NotFound();
        return PartialView("_FormHorario", vm);
    }

    // GET /Horarios/GetById/5 — retorna JSON del horario para editar
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var vm = await _service.GetHorarioByIdAsync(id);
        if (vm == null) return NotFound();
        return Json(vm);
    }

    // POST /Horarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] HorarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message, newId) = await _service.CreateHorarioAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    // POST /Horarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] HorarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message) = await _service.UpdateHorarioAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // POST /Horarios/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteHorarioAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // POST /Horarios/ToggleActivo/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var (success, message) = await _service.ToggleActivoAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }
}