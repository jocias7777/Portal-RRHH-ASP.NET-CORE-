using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IAsistenciaService
{
    Task<DataTablesResponse<AsistenciaListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<AsistenciaViewModel?> GetByIdAsync(int id);
    Task<(bool success, string message, int newId)> CreateAsync(AsistenciaViewModel vm);
    Task<(bool success, string message)> UpdateAsync(int id, AsistenciaViewModel vm);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<AsistenciaKpiViewModel> GetKpisHoyAsync();

    // Horarios
    Task<DataTablesResponse<HorarioListViewModel>> GetHorariosDataTablesAsync(DataTablesRequest request);
    Task<HorarioViewModel?> GetHorarioByIdAsync(int id);
    Task<(bool success, string message, int newId)> CreateHorarioAsync(HorarioViewModel vm);
    Task<(bool success, string message)> UpdateHorarioAsync(int id, HorarioViewModel vm);
    Task<(bool success, string message)> DeleteHorarioAsync(int id);
    Task<(bool success, string message)> ToggleActivoAsync(int id);
}