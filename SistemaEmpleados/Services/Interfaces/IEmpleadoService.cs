using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IEmpleadoService
{
    Task<DataTablesResponse<EmpleadoListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<EmpleadoDetalleViewModel?> GetByIdAsync(int id);
    Task<(bool success, string message, int id)> CreateAsync(EmpleadoViewModel vm);
    Task<(bool success, string message)> UpdateAsync(int id, EmpleadoViewModel vm);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<IEnumerable<object>> SearchForGlobalAsync(string term);
    Task<string> GenerateNextCodigoAsync();
}