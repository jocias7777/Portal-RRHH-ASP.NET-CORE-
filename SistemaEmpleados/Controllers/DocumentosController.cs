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
public class DocumentosController : Controller
{
    private readonly IDocumentoService _service;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentosController(IDocumentoService service, ApplicationDbContext context, IWebHostEnvironment env)
    {
        _service = service;
        _context = context;
        _env = env;
    }

    public IActionResult Index()
    {
        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView() : View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int? empleadoId, int? departamentoId, string? tipo, string? estado)
    {
        try
        {
            var query = _context.Documentos
                .Include(d => d.Empleado)
                .ThenInclude(e => e.Departamento)
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            if (empleadoId.HasValue && empleadoId > 0)
                query = query.Where(d => d.EmpleadoId == empleadoId);

            if (departamentoId.HasValue && departamentoId > 0)
                query = query.Where(d => d.Empleado.DepartamentoId == departamentoId);

            if (!string.IsNullOrWhiteSpace(tipo) &&
                Enum.TryParse<TipoDocumento>(tipo, out var tipoEnum))
                query = query.Where(d => d.Tipo == tipoEnum);

            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoDocumento>(estado, out var estadoEnum))
                query = query.Where(d => d.Estado == estadoEnum);

            var data = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var result = data.Select(d => new
            {
                id = d.Id,
                titulo = d.Titulo,
                descripcion = d.Descripcion,
                tipo = d.Tipo.ToString(),
                modalidad = d.Modalidad?.ToString(),
                estado = d.Estado.ToString(),
                urlArchivo = d.UrlArchivo,
                nombreArchivo = d.NombreArchivo,
                numeroFolio = d.NumeroFolio,
                fechaExpiracion = d.FechaExpiracion?.ToString("dd/MM/yyyy"),
                createdAt = d.CreatedAt.ToString("dd/MM/yyyy"),
                empleadoId = d.EmpleadoId,
                empleadoNombre = d.Empleado.NombreCompleto,
                empleadoDepartamento = d.Empleado.Departamento?.Nombre,
                estaExpirado = d.FechaExpiracion.HasValue && d.FechaExpiracion < DateTime.Today,
                porExpirar = d.FechaExpiracion.HasValue && d.FechaExpiracion >= DateTime.Today && d.FechaExpiracion <= DateTime.Today.AddDays(30)
            });

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, data = Array.Empty<object>(), message = ex.Message });
        }
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
        var vm = id.HasValue
            ? (DocumentoViewModel?)await _service.GetFormViewModelAsync(id.Value)
            : new DocumentoViewModel();

        if (vm == null) return NotFound();

        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Expediente(int id)
    {
        var vm = await _service.GetExpedienteEmpleadoAsync(id);
        return PartialView("_Expediente", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Alertas()
    {
        var alertas = await _service.GetAlertasAsync();
        return PartialView("_Alertas", alertas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromForm] DocumentoViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(k => k.Key, v => v.Value!.Errors.Select(e => e.ErrorMessage).ToList());
            return Json(ApiResponse.Fail("Datos inválidos: " + string.Join(", ", errors.Values.SelectMany(x => x)),
                errors.Values.SelectMany(x => x).ToList()));
        }

        var (success, message, newId) = await _service.CreateAsync(vm, vm.Archivo);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromForm] DocumentoViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message) = await _service.UpdateAsync(id, vm, vm.Archivo);
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
    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _service.GetByIdAsync(id);
        if (vm == null) return NotFound();

        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView("_Detalle", vm) : View("Detalle", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Descargar(int id)
    {
        var doc = await _context.Documentos.FindAsync(id);
        if (doc == null || string.IsNullOrEmpty(doc.NombreArchivo))
            return NotFound();

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "documentos");
        var filePath = Path.Combine(uploadsPath, doc.NombreArchivo);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = doc.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, doc.NombreArchivo);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { groups = Array.Empty<object>() });

        var results = await _service.SearchForGlobalAsync(q);
        return Json(new
        {
            groups = new[] { new { modulo = "Documentos", items = results } }
        });
    }

    private async Task CargarSelectLists()
    {
        ViewBag.Empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .OrderBy(e => e.PrimerApellido)
            .Select(e => new { e.Id, nombre = e.PrimerNombre + " " + e.PrimerApellido, e.DepartamentoId, departamento = e.Departamento.Nombre })
            .ToListAsync();

        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
    }
}