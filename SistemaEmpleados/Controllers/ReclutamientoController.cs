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
public class ReclutamientoController : Controller
{
    private readonly IReclutamientoService _service;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ReclutamientoController(
        IReclutamientoService service,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    public IActionResult Index()
    {
        var isPartial =
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView() : View();
    }

    // ── Estadísticas KPI ──────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Estadisticas()
    {
        var vm = await _service.GetEstadisticasAsync();
        return Json(vm);
    }

    // ── PLAZAS ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> GetPlazas(
        [FromBody] DataTablesRequest request)
    {
        var result = await _service.GetPlazasDataTablesAsync(request);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> DetallePlaza(int id)
    {
        var vm = await _service.GetPlazaDetalleAsync(id);
        if (vm == null) return Json(ApiResponse.Fail("Plaza no encontrada."));

        // Este endpoint se consume vía fetch desde `reclutamiento.js`
        // y debe responder JSON, no vistas parciales.
        return Json(vm);
    }


    [HttpGet]
    public async Task<IActionResult> FormPlaza(int? id)
    {
        var vm = id.HasValue
            ? await _service.GetPlazaByIdAsync(id.Value)
            : new PlazaVacanteViewModel
            { FechaPublicacion = DateTime.Today };

        if (vm == null) return NotFound();
        await CargarSelectLists();
        return PartialView("_FormPlaza", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CreatePlaza(
        [FromBody] PlazaVacanteViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var (success, message, newId) =
            await _service.CreatePlazaAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EditPlaza(
        int id, [FromBody] PlazaVacanteViewModel vm)
    {
        var (success, message) =
            await _service.UpdatePlazaAsync(id, vm);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> DeletePlaza(int id)
    {
        var (success, message) = await _service.DeletePlazaAsync(id);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CambiarEstadoPlaza(
        int id, [FromBody] CambiarEstadoPlazaViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        var usuario = user?.UserName ?? User.Identity?.Name ?? "Sistema";
        var (success, message) =
            await _service.CambiarEstadoPlazaAsync(id, vm, usuario);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    // ── CANDIDATOS ────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> GetCandidatos(
        [FromBody] DataTablesRequest request)
    {
        var result =
            await _service.GetCandidatosDataTablesAsync(request);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> FormCandidato(
        int? id, int? plazaId)
    {
        CandidatoViewModel vm;
        if (id.HasValue)
        {
            var found = await _service.GetCandidatoByIdAsync(id.Value);
            if (found == null) return NotFound();
            vm = found;
        }
        else
        {
            vm = new CandidatoViewModel
            {
                PlazaVacanteId = plazaId ?? 0,
                FechaEntrevista = null
            };
        }

        ViewBag.Plazas = await _context.PlazasVacantes
            .Where(p => !p.IsDeleted
                     && (p.Estado == EstadoPlaza.Abierta
                      || p.Estado == EstadoPlaza.EnProceso))
            .OrderBy(p => p.Titulo)
            .Select(p => new { p.Id, p.Titulo })
            .ToListAsync();

        return PartialView("_FormCandidato", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CreateCandidato(
        [FromForm] CandidatoViewModel vm,
        IFormFile? cvFile)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        if (cvFile is { Length: > 0 })
        {
            vm.CvUrl = await GuardarCvAsync(cvFile);
        }

        var (success, message, newId) =
            await _service.CreateCandidatoAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EditCandidato(
        int id,
        [FromForm] CandidatoViewModel vm,
        IFormFile? cvFile)
    {
        if (cvFile is { Length: > 0 })
        {
            vm.CvUrl = await GuardarCvAsync(cvFile);
        }

        var (success, message) =
            await _service.UpdateCandidatoAsync(id, vm);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> DeleteCandidato(int id)
    {
        var (success, message) =
            await _service.DeleteCandidatoAsync(id);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CambiarEtapa(
        int id, [FromBody] int etapa)
    {
        var user = await _userManager.GetUserAsync(User);
        var usuario = user?.UserName ?? "Sistema";
        var (success, message) =
            await _service.CambiarEtapaAsync(id, etapa, usuario);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> RegistrarOferta(
        [FromBody] OfertaCandidatoViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        var usuario = user?.UserName ?? "Sistema";
        var (success, message) =
            await _service.RegistrarOfertaAsync(vm, usuario);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }


    // ── ENTREVISTAS ───────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetEntrevistas(int candidatoId)
    {
        var data =
            await _service.GetEntrevistasCandidatoAsync(candidatoId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CrearEntrevista(
        [FromBody] EntrevistaViewModel vm)
    {
        var (success, message) =
            await _service.CrearEntrevistaAsync(vm);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> ActualizarEntrevista(
        int id, [FromBody] EntrevistaViewModel vm)
    {
        var (success, message) =
            await _service.ActualizarResultadoEntrevistaAsync(id, vm);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EliminarEntrevista(int id)
    {
        var (success, message) =
            await _service.EliminarEntrevistaAsync(id);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    // ── NOTAS ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetNotas(int candidatoId)
    {
        var data =
            await _service.GetNotasCandidatoAsync(candidatoId);
        return Json(new { success = true, data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> AgregarNota(
        [FromBody] NotaCandidatoViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        var usuario = user?.UserName ?? "Sistema";
        var (success, message) =
            await _service.AgregarNotaAsync(vm, usuario);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EliminarNota(int id)
    {
        var (success, message) = await _service.EliminarNotaAsync(id);
        return Json(success
            ? ApiResponse.Ok(message)
            : ApiResponse.Fail(message));
    }

    // ── CONVERTIR EN EMPLEADO ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> ConvertirEnEmpleado(
        [FromBody] ConvertirEmpleadoViewModel vm)
    {
        var (success, message, empleadoId) =
            await _service.ConvertirEnEmpleadoAsync(vm);
        return Json(success
            ? ApiResponse<object>.Ok(
                new { empleadoId }, message)
            : ApiResponse<object>.Fail(message));
    }

    // ── Helpers ───────────────────────────────────────────
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

    private async Task<string> GuardarCvAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "cv");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"cv_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/cv/{fileName}";
    }
}