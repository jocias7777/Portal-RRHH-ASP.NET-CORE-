using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IPrestacionService
{
    Task<DataTablesResponse<PrestacionListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<PrestacionViewModel?> GetByIdAsync(int id);
    Task<(bool success, string message, int id)> CreateAsync(PrestacionViewModel vm, string calculadoPor);
    Task<(bool success, string message)> UpdateAsync(int id, PrestacionViewModel vm);
    Task<(bool success, string message)> MarcarPagadoAsync(int id, DateTime fechaPago);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<CalculoPrestacionViewModel?> CalcularPrestacionesAsync(int empleadoId, int anio = 0);
    Task<(bool success, string message, int cantidad)> GenerarPrestacionesAnioAsync(int anio, string calculadoPor, int? departamentoId = null);
}