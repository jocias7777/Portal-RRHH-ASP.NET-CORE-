using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IDocumentoService
{
    Task<DataTablesResponse<DocumentoListViewModel>> GetDataTablesAsync(DataTablesRequest request);
    Task<DocumentoDetalleViewModel?> GetByIdAsync(int id);
    Task<DocumentoViewModel?> GetFormViewModelAsync(int id);
    Task<(bool success, string message, int id)> CreateAsync(DocumentoViewModel vm, IFormFile? archivo = null);
    Task<(bool success, string message)> UpdateAsync(int id, DocumentoViewModel vm, IFormFile? archivo = null);
    Task<(bool success, string message)> DeleteAsync(int id);
    Task<IEnumerable<object>> SearchForGlobalAsync(string term);
    Task<IEnumerable<object>> GetExpiringAsync(int dias = 30);
    Task<int> GetTotalCountAsync();
    Task<List<DocumentoAlertaViewModel>> GetAlertasAsync();
    Task<ExpedienteEmpleadoViewModel> GetExpedienteEmpleadoAsync(int empleadoId);
}