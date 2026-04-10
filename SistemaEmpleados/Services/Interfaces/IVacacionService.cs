using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IVacacionService
{
    Task<DataTablesResponse<VacacionListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<VacacionViewModel?> GetByIdAsync(int id);
    Task<(bool success, string message, int id)> CreateAsync(VacacionViewModel vm, string creadoPor);
    Task<(bool success, string message)> UpdateAsync(int id, VacacionViewModel vm);
    Task<(bool success, string message)> AprobarAsync(int id, string aprobadoPor);
    Task<(bool success, string message)> RechazarAsync(int id, string motivo);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<int> GetDiasDisponiblesAsync(int empleadoId, int anio);

    // ── NUEVO: Ausencias ──
    Task<List<AusenciaListViewModel>> GetAusenciasAsync();
    Task<(bool success, string message)> CreateAusenciaAsync(AusenciaViewModel vm);
    Task<(bool success, string message)> DeleteAusenciaAsync(int id);

    // ── NUEVO: Saldos ──
    Task<List<SaldoVacacionViewModel>> GetSaldosAsync();

    // ── NUEVO: KPIs ──
    Task<VacacionKpiViewModel> GetKPIsAsync();
}