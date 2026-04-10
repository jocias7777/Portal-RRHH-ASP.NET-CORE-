using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IReportesService
{
    // ════════════════════════════════════════════
    // NÓMINA Y COSTOS LABORALES
    // ════════════════════════════════════════════
    Task<List<ReportePlanillaMensualViewModel>> GetPlanillaMensualAsync(int mes, int anio, int? departamentoId = null);
    Task<List<ResumenBono14AguinaldoViewModel>> GetBono14AguinaldoProyectadoAsync(int? departamentoId = null);
    Task<List<CostoPorDepartamentoViewModel>> GetCostoPorDepartamentoAsync(int anio);

    // ════════════════════════════════════════════
    // CUMPLIMIENTO LEGAL
    // ════════════════════════════════════════════
    Task<List<ContratoPorVencerViewModel>> GetContratosPorVencerAsync(int dias = 90);
    Task<List<EmpleadoSinDocumentosViewModel>> GetEmpleadosSinDocumentosAsync();
    Task<ReporteNacionalidadViewModel> GetNacionalidadReporteAsync();
    Task<List<VacacionesNoTomadasViewModel>> GetVacacionesNoTomadasAsync();

    // ════════════════════════════════════════════
    // ASISTENCIA Y TIEMPO
    // ════════════════════════════════════════════
    Task<List<ReporteInasistenciasTardanzasViewModel>> GetInasistenciasTardanzasAsync(DateTime desde, DateTime hasta, int? departamentoId = null);
    Task<List<HorasExtraPorEmpleadoViewModel>> GetHorasExtraPorEmpleadoAsync(DateTime desde, DateTime hasta, int? departamentoId = null);
    Task<List<EmpleadoConMasDeTresFaltasViewModel>> GetEmpleadosConMasDeTresFaltasAsync(int mes, int anio);

    // ════════════════════════════════════════════
    // ROTACIÓN DE PERSONAL
    // ════════════════════════════════════════════
    Task<List<AltasBajasViewModel>> GetAltasBajasPorPeriodoAsync(DateTime desde, DateTime hasta);
    Task<List<MotivoSalidaViewModel>> GetMotivosSalidaAsync(DateTime desde, DateTime hasta);
    Task<List<TiempoPermanenciaViewModel>> GetTiempoPermanenciaPorDepartamentoAsync();

    // ════════════════════════════════════════════
    // PRESTACIONES E INDEMNIZACIONES
    // ════════════════════════════════════════════
    Task<List<ProyeccionIndemnizacionViewModel>> GetProyeccionIndemnizacionAsync(int? departamentoId = null);
    Task<List<VacacionesAcumuladasViewModel>> GetVacacionesAcumuladasValorizadasAsync(int? departamentoId = null);
    Task<List<FiniquitoEmitidoViewModel>> GetFiniquitosEmitidosAsync(DateTime desde, DateTime hasta);

    // ════════════════════════════════════════════
    // EXPEDIENTES
    // ════════════════════════════════════════════
    Task<List<CompletitudExpedienteViewModel>> GetCompletitudExpedientesAsync();
    Task<List<DocumentoVencidoViewModel>> GetDocumentosVencidosOPorVencerAsync(int dias = 30);

    // ════════════════════════════════════════════
    // PROGRAMACIÓN DE REPORTES
    // ════════════════════════════════════════════
    Task<List<ReporteProgramadoViewModel>> GetReportesProgramadosAsync();
    Task<ReporteProgramadoViewModel?> GetReporteProgramadoByIdAsync(int id);
    Task<ReporteProgramado> CrearReporteProgramadoAsync(CrearReporteProgramadoViewModel model);
    Task<bool> ActualizarReporteProgramadoAsync(int id, CrearReporteProgramadoViewModel model);
    Task<bool> EliminarReporteProgramadoAsync(int id);
    Task<bool> ActivarDesactivarReporteProgramadoAsync(int id, bool activo);
    Task<List<ReporteProgramado>> GetReportesPorEnviarAsync();
    Task MarcarEnvioExitosoAsync(int id, DateTime fecha);
    Task MarcarEnvioFallidoAsync(int id, string error);
    Task<byte[]> GenerarExcelReporteAsync(string tipoReporte, int? departamentoId = null);
}
