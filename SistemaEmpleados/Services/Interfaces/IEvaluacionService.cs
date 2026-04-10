using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IEvaluacionService
{
    Task<DataTablesResponse<EvaluacionListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<EvaluacionViewModel?> GetByIdAsync(int id);
    Task<(bool success, string message, int id)> CreateAsync(EvaluacionViewModel vm, string evaluadorId);
    Task<(bool success, string message)> UpdateAsync(int id, EvaluacionViewModel vm);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<IEnumerable<KPIListViewModel>> GetKPIsAsync();
    Task<(bool success, string message, int id)> CreateKPIAsync(KPIViewModel vm);
    Task<(bool success, string message)> UpdateKPIAsync(int id, KPIViewModel vm);
    Task<(bool success, string message)> DeleteKPIAsync(int id);
    Task<IEnumerable<ResultadoKPIViewModel>> GetKPIsParaEmpleadoAsync(int empleadoId);
}
