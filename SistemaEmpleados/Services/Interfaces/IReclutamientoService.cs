using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.ViewModels;

namespace SistemaEmpleados.Services.Interfaces;

public interface IReclutamientoService
{
    // ── Plazas ──
    Task<DataTablesResponse<PlazaVacanteListViewModel>>
        GetPlazasDataTablesAsync(DataTablesRequest req);
    Task<PlazaVacanteViewModel?> GetPlazaByIdAsync(int id);
    Task<PlazaDetalleViewModel?> GetPlazaDetalleAsync(int id);
    Task<(bool success, string message, int id)>
        CreatePlazaAsync(PlazaVacanteViewModel vm);
    Task<(bool success, string message)>
        UpdatePlazaAsync(int id, PlazaVacanteViewModel vm);
    Task<(bool success, string message)> DeletePlazaAsync(int id);
    Task<(bool success, string message)>
        CambiarEstadoPlazaAsync(int id,
            CambiarEstadoPlazaViewModel vm, string usuario);

    // ── Candidatos ──
    Task<DataTablesResponse<CandidatoListViewModel>>
        GetCandidatosDataTablesAsync(DataTablesRequest req);
    Task<CandidatoViewModel?> GetCandidatoByIdAsync(int id);
    Task<(bool success, string message, int id)>
        CreateCandidatoAsync(CandidatoViewModel vm);
    Task<(bool success, string message)>
        UpdateCandidatoAsync(int id, CandidatoViewModel vm);
    Task<(bool success, string message)> DeleteCandidatoAsync(int id);
    Task<(bool success, string message)>
        CambiarEtapaAsync(int id, int etapa, string usuario);
    Task<(bool success, string message)>
        RegistrarOfertaAsync(OfertaCandidatoViewModel vm, string usuario);


    // ── Entrevistas ──
    Task<IEnumerable<EntrevistaViewModel>>
        GetEntrevistasCandidatoAsync(int candidatoId);
    Task<(bool success, string message)>
        CrearEntrevistaAsync(EntrevistaViewModel vm);
    Task<(bool success, string message)>
        ActualizarResultadoEntrevistaAsync(int id,
            EntrevistaViewModel vm);
    Task<(bool success, string message)>
        EliminarEntrevistaAsync(int id);

    // ── Notas ──
    Task<IEnumerable<NotaCandidatoViewModel>>
        GetNotasCandidatoAsync(int candidatoId);
    Task<(bool success, string message)>
        AgregarNotaAsync(NotaCandidatoViewModel vm, string usuario);
    Task<(bool success, string message)> EliminarNotaAsync(int id);

    // ── Convertir a empleado ──
    Task<(bool success, string message, int empleadoId)>
        ConvertirEnEmpleadoAsync(ConvertirEmpleadoViewModel vm);

    // ── Estadísticas ──
    Task<EstadisticasReclutamientoViewModel> GetEstadisticasAsync();
}