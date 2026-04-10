using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class ReportesController : Controller
{
    private readonly IReportesService _reportesService;
    private readonly ApplicationDbContext _context;

    public ReportesController(IReportesService reportesService, ApplicationDbContext context)
    {
        _reportesService = reportesService;
        _context = context;
    }

    // ── GET: /Reportes
    public IActionResult Index()
    {
        return View();
    }

    // ════════════════════════════════════════════
    // NÓMINA Y COSTOS LABORALES
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> PlanillaMensual(int mes, int anio, int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetPlanillaMensualAsync(mes, anio, departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Bono14Aguinaldo(int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetBono14AguinaldoProyectadoAsync(departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CostoPorDepartamento(int anio)
    {
        try
        {
            var data = await _reportesService.GetCostoPorDepartamentoAsync(anio);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // CUMPLIMIENTO LEGAL
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ContratosPorVencer(int dias = 90)
    {
        try
        {
            var data = await _reportesService.GetContratosPorVencerAsync(dias);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosSinDocumentos()
    {
        try
        {
            var data = await _reportesService.GetEmpleadosSinDocumentosAsync();
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Nacionalidad()
    {
        try
        {
            var data = await _reportesService.GetNacionalidadReporteAsync();
            return Json(new { success = true, data = new[] { data } });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> VacacionesNoTomadas()
    {
        try
        {
            var data = await _reportesService.GetVacacionesNoTomadasAsync();
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // ASISTENCIA Y TIEMPO
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> InasistenciasTardanzas(DateTime desde, DateTime hasta, int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetInasistenciasTardanzasAsync(desde, hasta, departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> HorasExtra(DateTime desde, DateTime hasta, int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetHorasExtraPorEmpleadoAsync(desde, hasta, departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosConMasDeTresFaltas(int mes, int anio)
    {
        try
        {
            var data = await _reportesService.GetEmpleadosConMasDeTresFaltasAsync(mes, anio);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // ROTACIÓN DE PERSONAL
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> AltasBajas(DateTime desde, DateTime hasta)
    {
        try
        {
            var data = await _reportesService.GetAltasBajasPorPeriodoAsync(desde, hasta);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MotivosSalida(DateTime desde, DateTime hasta)
    {
        try
        {
            var data = await _reportesService.GetMotivosSalidaAsync(desde, hasta);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> TiempoPermanencia()
    {
        try
        {
            var data = await _reportesService.GetTiempoPermanenciaPorDepartamentoAsync();
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // PRESTACIONES E INDEMNIZACIONES
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ProyeccionIndemnizacion(int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetProyeccionIndemnizacionAsync(departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> VacacionesAcumuladas(int? departamentoId = null)
    {
        try
        {
            var data = await _reportesService.GetVacacionesAcumuladasValorizadasAsync(departamentoId);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> FiniquitosEmitidos(DateTime desde, DateTime hasta)
    {
        try
        {
            var data = await _reportesService.GetFiniquitosEmitidosAsync(desde, hasta);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // EXPEDIENTES
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> CompletitudExpedientes()
    {
        try
        {
            var data = await _reportesService.GetCompletitudExpedientesAsync();
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DocumentosVencidos(int dias = 30)
    {
        try
        {
            var data = await _reportesService.GetDocumentosVencidosOPorVencerAsync(dias);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ════════════════════════════════════════════
    // VISTAS PARCIALES PARA CADA TIPO DE REPORTE
    // ════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> ReporteNomina()
    {
        ViewBag.FechaDesde = DateTime.Today.AddMonths(-1);
        ViewBag.FechaHasta = DateTime.Today;
        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
        return PartialView("_ReporteNomina");
    }

    [HttpGet]
    public async Task<IActionResult> ReporteCumplimientoLegal()
    {
        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
        return PartialView("_ReporteCumplimientoLegal");
    }

    [HttpGet]
    public async Task<IActionResult> ReporteAsistencia()
    {
        ViewBag.FechaDesde = DateTime.Today.AddMonths(-1);
        ViewBag.FechaHasta = DateTime.Today;
        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
        return PartialView("_ReporteAsistencia");
    }

    [HttpGet]
    public IActionResult ReporteRotacion()
    {
        ViewBag.FechaDesde = DateTime.Today.AddYears(-1);
        ViewBag.FechaHasta = DateTime.Today;
        return PartialView("_ReporteRotacion");
    }

    [HttpGet]
    public async Task<IActionResult> ReportePrestaciones()
    {
        ViewBag.Departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
        return PartialView("_ReportePrestaciones");
    }

    [HttpGet]
    public IActionResult ReporteExpedientes()
    {
        return PartialView("_ReporteExpedientes");
    }

    [HttpGet]
    public async Task<IActionResult> GetReportesProgramados()
    {
        try
        {
            var reportes = await _reportesService.GetReportesProgramadosAsync();
            return Json(new { success = true, data = reportes });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetReporteProgramado(int id)
    {
        try
        {
            var reporte = await _reportesService.GetReporteProgramadoByIdAsync(id);
            if (reporte == null)
                return Json(new { success = false, message = "Reporte no encontrado" });
            return Json(new { success = true, data = reporte });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CrearReporteProgramado([FromBody] CrearReporteProgramadoViewModel model)
    {
        try
        {
            var entity = await _reportesService.CrearReporteProgramadoAsync(model);
            return Json(new { success = true, message = "Reporte programado creado exitosamente", id = entity.Id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> ActualizarReporteProgramado(int id, [FromBody] CrearReporteProgramadoViewModel model)
    {
        try
        {
            var result = await _reportesService.ActualizarReporteProgramadoAsync(id, model);
            if (!result)
                return Json(new { success = false, message = "Reporte no encontrado" });
            return Json(new { success = true, message = "Reporte actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> EliminarReporteProgramado(int id)
    {
        try
        {
            var result = await _reportesService.EliminarReporteProgramadoAsync(id);
            if (!result)
                return Json(new { success = false, message = "Reporte no encontrado" });
            return Json(new { success = true, message = "Reporte eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ActivarDesactivarReporteProgramado(int id, bool activo)
    {
        try
        {
            var result = await _reportesService.ActivarDesactivarReporteProgramadoAsync(id, activo);
            if (!result)
                return Json(new { success = false, message = "Reporte no encontrado" });
            return Json(new { success = true, message = activo ? "Reporte activado" : "Reporte desactivado" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerDepartamentos()
    {
        var deptos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();
        return Json(new { success = true, data = deptos });
    }
}
