using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface INominaService
{
    Task<DataTablesResponse> GetDataTablesAsync(DataTablesRequest request);
    Task<PlanillaListViewModel?> GetByIdAsync(int id);
    Task<IEnumerable<DetallePlanillaViewModel>> GetDetallesAsync(int planillaId);
    Task<(bool success, string message, int newId)> GenerarPlanillaAsync(int mes, int anio, string generadoPor);
    Task<(bool success, string message)> MarcarPagadaAsync(int id, DateTime fechaPago);
    Task<(bool success, string message)> AnularAsync(int id);
    Task<(bool success, string message)> ActualizarDetalleAsync(int id, DetallePlanillaEditViewModel vm);
    Task<ResumenNominaViewModel> GetResumenAnioAsync(int anio);
    Task<IEnumerable<PlanillaListViewModel>> GetHistorialEmpleadoAsync(int empleadoId);
    // Eliminado método con error de tipeo
    Task<BoletaPagoViewModel?> GetBoletaPagoAsync(int detallePlanillaId);

    // Salarios
    Task<(bool success, string message)> ActualizarSalarioAsync(
        int empleadoId, ActualizarSalarioViewModel vm);

    // Préstamos
    Task<IEnumerable<PrestamoListViewModel>> GetPrestamosAsync();
    Task<(bool success, string message)> CrearPrestamoAsync(PrestamoViewModel vm);
    Task<(bool success, string message)> CancelarPrestamoAsync(int id);

    // Conceptos
    Task<IEnumerable<ConceptoListViewModel>> GetConceptosAsync();
    Task<(bool success, string message)> CrearConceptoAsync(ConceptoNominaViewModel vm);
    Task<(bool success, string message)> EditarConceptoAsync(
        int id, ConceptoNominaViewModel vm);
    Task<(bool success, string message)> EliminarConceptoAsync(int id);
    Task<(bool success, string message)> EliminarPrestamoAsync(int id);
    Task<(bool success, string message)> AbonarCuotaAsync(int id, decimal monto);
    Task<(bool success, string message)> PagarDeudaCompletaAsync(int id);
    Task<(bool success, string message)> EliminarPlanillaAsync(int id);

}