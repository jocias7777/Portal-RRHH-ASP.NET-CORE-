using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class EvaluacionService : IEvaluacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEvaluacionRepository _repo;
    private readonly IKPIRepository _kpiRepo;
    private readonly IUnitOfWork _uow;

    public EvaluacionService(
        ApplicationDbContext context,
        IEvaluacionRepository repo,
        IKPIRepository kpiRepo,
        IUnitOfWork uow)
    {
        _context = context;
        _repo = repo;
        _kpiRepo = kpiRepo;
        _uow = uow;
    }

    public async Task<DataTablesResponse<EvaluacionListViewModel>> GetDataTablesAsync(
        DataTablesRequest req)
    {
        var query = _context.Evaluaciones
            .Include(e => e.Empleado).ThenInclude(em => em.Departamento)
            .Include(e => e.Empleado).ThenInclude(em => em.Puesto)
            .Include(e => e.Evaluador)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(e =>
                e.Empleado.PrimerNombre.ToLower().Contains(s) ||
                e.Empleado.PrimerApellido.ToLower().Contains(s) ||
                e.Periodo.ToLower().Contains(s));
        }

        if (req.DepartamentoId.HasValue)
            query = query.Where(e => e.Empleado.DepartamentoId == req.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EstadoEvaluacion>(req.Estado, out var estado))
            query = query.Where(e => e.Estado == estado);

        var total = await query.CountAsync();
        query = query.OrderByDescending(e => e.FechaEvaluacion);

        var data = await query
            .Skip(req.Start)
            .Take(req.Length)
            .Select(e => new EvaluacionListViewModel
            {
                Id = e.Id,
                NombreEmpleado = $"{e.Empleado.PrimerNombre} {e.Empleado.PrimerApellido}".Trim(),
                Iniciales = $"{e.Empleado.PrimerNombre[0]}{e.Empleado.PrimerApellido[0]}".ToUpper(),
                FotoUrl = e.Empleado.FotoUrl,
                Departamento = e.Empleado.Departamento.Nombre,
                Puesto = e.Empleado.Puesto.Nombre,
                NombreEvaluador = e.Evaluador.NombreCompleto,
                Periodo = e.Periodo,
                TipoEvaluacion = $"{(int)e.TipoEvaluacion}°",
                PuntajeTotal = e.PuntajeTotal,
                Estado = e.Estado.ToString(),
                FechaEvaluacion = e.FechaEvaluacion.ToString("dd/MM/yyyy"),
                Calificacion = ObtenerCalificacion(e.PuntajeTotal)
            })
            .ToListAsync();

        return new DataTablesResponse<EvaluacionListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    public async Task<EvaluacionViewModel?> GetByIdAsync(int id)
    {
        var e = await _repo.GetByIdWithRelationsAsync(id);
        if (e == null) return null;

        return new EvaluacionViewModel
        {
            Id = e.Id,
            EmpleadoId = e.EmpleadoId,
            Periodo = e.Periodo,
            TipoEvaluacion = e.TipoEvaluacion,
            Estado = e.Estado,
            FechaEvaluacion = e.FechaEvaluacion,
            Comentarios = e.Comentarios,
            PlanMejora = e.PlanMejora,
            Resultados = e.Resultados.Select(r => new ResultadoKPIViewModel
            {
                Id = r.Id,
                KPIId = r.KPIId,
                NombreKPI = r.KPI.Nombre,
                PesoKPI = r.KPI.Peso,
                Calificacion = r.Calificacion,
                Observacion = r.Observacion
            }).ToList()
        };
    }

    public async Task<(bool success, string message, int id)> CreateAsync(
        EvaluacionViewModel vm, string evaluadorId)
    {
        var evaluacion = new Evaluacion
        {
            EmpleadoId = vm.EmpleadoId,
            EvaluadorId = evaluadorId,
            Periodo = vm.Periodo,
            TipoEvaluacion = vm.TipoEvaluacion,
            Estado = vm.Estado,
            FechaEvaluacion = vm.FechaEvaluacion,
            Comentarios = vm.Comentarios,
            PlanMejora = vm.PlanMejora,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calcular resultados y puntaje
        decimal puntajeTotal = 0;
        foreach (var r in vm.Resultados)
        {
            var kpi = await _kpiRepo.GetByIdAsync(r.KPIId);
            if (kpi == null) continue;

            var ponderado = Math.Round(r.Calificacion * kpi.Peso / 100, 2);
            puntajeTotal += ponderado;

            evaluacion.Resultados.Add(new ResultadoKPI
            {
                KPIId = r.KPIId,
                Calificacion = r.Calificacion,
                PuntajePonderado = ponderado,
                Observacion = r.Observacion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        evaluacion.PuntajeTotal = Math.Round(puntajeTotal, 2);
        if (vm.Resultados.Any()) evaluacion.Estado = EstadoEvaluacion.Completada;

        await _repo.AddAsync(evaluacion);
        await _uow.SaveChangesAsync();

        return (true, "Evaluación registrada correctamente.", evaluacion.Id);
    }

    public async Task<(bool success, string message)> UpdateAsync(int id, EvaluacionViewModel vm)
    {
        var evaluacion = await _repo.GetByIdWithRelationsAsync(id);
        if (evaluacion == null) return (false, "Evaluación no encontrada.");

        evaluacion.EmpleadoId = vm.EmpleadoId;
        evaluacion.Periodo = vm.Periodo;
        evaluacion.TipoEvaluacion = vm.TipoEvaluacion;
        evaluacion.Estado = vm.Estado;
        evaluacion.FechaEvaluacion = vm.FechaEvaluacion;
        evaluacion.Comentarios = vm.Comentarios;
        evaluacion.PlanMejora = vm.PlanMejora;
        evaluacion.UpdatedAt = DateTime.UtcNow;

        // Actualizar resultados
        foreach (var r in vm.Resultados)
        {
            var existing = evaluacion.Resultados.FirstOrDefault(x => x.KPIId == r.KPIId);
            var kpi = await _kpiRepo.GetByIdAsync(r.KPIId);
            if (kpi == null) continue;

            var ponderado = Math.Round(r.Calificacion * kpi.Peso / 100, 2);

            if (existing != null)
            {
                existing.Calificacion = r.Calificacion;
                existing.PuntajePonderado = ponderado;
                existing.Observacion = r.Observacion;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                evaluacion.Resultados.Add(new ResultadoKPI
                {
                    KPIId = r.KPIId,
                    Calificacion = r.Calificacion,
                    PuntajePonderado = ponderado,
                    Observacion = r.Observacion,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        evaluacion.PuntajeTotal = Math.Round(
            evaluacion.Resultados.Sum(r => r.PuntajePonderado), 2);

        _repo.Update(evaluacion);
        await _uow.SaveChangesAsync();

        return (true, "Evaluación actualizada correctamente.");
    }

    public async Task<(bool success, string message)> DeleteAsync(int id)
    {
        var evaluacion = await _repo.GetByIdAsync(id);
        if (evaluacion == null) return (false, "Evaluación no encontrada.");

        evaluacion.IsDeleted = true;
        evaluacion.UpdatedAt = DateTime.UtcNow;
        _repo.Update(evaluacion);
        await _uow.SaveChangesAsync();

        return (true, "Evaluación eliminada correctamente.");
    }

    public async Task<IEnumerable<KPIListViewModel>> GetKPIsAsync()
    {
        return await _context.KPIs
            .Include(k => k.Puesto)
            .Where(k => !k.IsDeleted)
            .OrderBy(k => k.Nombre)
            .Select(k => new KPIListViewModel
            {
                Id = k.Id,
                Nombre = k.Nombre,
                Descripcion = k.Descripcion,
                Peso = k.Peso,
                Puesto = k.Puesto != null ? k.Puesto.Nombre : "General",
                Activo = k.Activo
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message, int id)> CreateKPIAsync(KPIViewModel vm)
    {
        var kpi = new KPI
        {
            Nombre = vm.Nombre.Trim(),
            Descripcion = vm.Descripcion,
            Peso = vm.Peso,
            PuestoId = vm.PuestoId,
            Activo = vm.Activo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _kpiRepo.AddAsync(kpi);
        await _uow.SaveChangesAsync();

        return (true, "KPI creado correctamente.", kpi.Id);
    }

    public async Task<(bool success, string message)> UpdateKPIAsync(int id, KPIViewModel vm)
    {
        var kpi = await _kpiRepo.GetByIdAsync(id);
        if (kpi == null) return (false, "KPI no encontrado.");

        kpi.Nombre = vm.Nombre.Trim();
        kpi.Descripcion = vm.Descripcion;
        kpi.Peso = vm.Peso;
        kpi.PuestoId = vm.PuestoId;
        kpi.Activo = vm.Activo;
        kpi.UpdatedAt = DateTime.UtcNow;

        _kpiRepo.Update(kpi);
        await _uow.SaveChangesAsync();

        return (true, "KPI actualizado correctamente.");
    }

    public async Task<(bool success, string message)> DeleteKPIAsync(int id)
    {
        var kpi = await _kpiRepo.GetByIdAsync(id);
        if (kpi == null) return (false, "KPI no encontrado.");

        kpi.IsDeleted = true;
        kpi.UpdatedAt = DateTime.UtcNow;
        _kpiRepo.Update(kpi);
        await _uow.SaveChangesAsync();

        return (true, "KPI eliminado correctamente.");
    }

    public async Task<IEnumerable<ResultadoKPIViewModel>> GetKPIsParaEmpleadoAsync(int empleadoId)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Id == empleadoId && !e.IsDeleted);

        var kpis = await _kpiRepo.GetActivosAsync(empleado?.PuestoId);

        return kpis.Select(k => new ResultadoKPIViewModel
        {
            KPIId = k.Id,
            NombreKPI = k.Nombre,
            PesoKPI = k.Peso,
            Calificacion = 0
        });
    }

    private static string ObtenerCalificacion(decimal puntaje) => puntaje switch
    {
        >= 90 => "Excelente",
        >= 75 => "Muy bueno",
        >= 60 => "Bueno",
        >= 45 => "Regular",
        _ => "Deficiente"
    };
}
