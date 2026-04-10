using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class VacacionService : IVacacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IVacacionRepository _repo;
    private readonly IUnitOfWork _uow;
    private const int DIAS_VACACIONES_GUATEMALA = 15;

    // Feriados nacionales Guatemala (mes, día)
    private static readonly (int mes, int dia)[] FeriadosGuatemala =
    {
        (1, 1),   // Año Nuevo
        (4, 1),   // Semana Santa (jueves)
        (4, 2),   // Semana Santa (viernes)
        (5, 1),   // Día del Trabajo
        (6, 30),  // Día del Ejército
        (9, 15),  // Independencia
        (10, 20), // Revolución
        (11, 1),  // Día de Todos los Santos
        (12, 24), // Noche Buena
        (12, 25), // Navidad
        (12, 31)  // Fin de año
    };

    public VacacionService(
        ApplicationDbContext context,
        IVacacionRepository repo,
        IUnitOfWork uow)
    {
        _context = context;
        _repo = repo;
        _uow = uow;
    }

    // ══════════════════════════════════════════
    // SOLICITUDES DE VACACIONES
    // ══════════════════════════════════════════

    public async Task<DataTablesResponse<VacacionListViewModel>> GetDataTablesAsync(DataTablesRequest req)
    {
        var query = _context.Vacaciones
            .Include(v => v.Empleado).ThenInclude(e => e.Departamento)
            .Where(v => !v.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(v =>
                v.Empleado.PrimerNombre.ToLower().Contains(s) ||
                v.Empleado.PrimerApellido.ToLower().Contains(s) ||
                v.Empleado.Codigo.ToLower().Contains(s));
        }

        if (req.DepartamentoId.HasValue)
            query = query.Where(v => v.Empleado.DepartamentoId == req.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EstadoVacacion>(req.Estado, out var estado))
            query = query.Where(v => v.Estado == estado);

        var total = await query.CountAsync();
        query = query.OrderByDescending(v => v.FechaSolicitud);

        var data = await query
            .Skip(req.Start)
            .Take(req.Length)
            .Select(v => new VacacionListViewModel
            {
                Id = v.Id,
                NombreEmpleado = $"{v.Empleado.PrimerNombre} {v.Empleado.PrimerApellido}".Trim(),
                Iniciales = $"{v.Empleado.PrimerNombre[0]}{v.Empleado.PrimerApellido[0]}".ToUpper(),
                FotoUrl = v.Empleado.FotoUrl,
                Departamento = v.Empleado.Departamento.Nombre,
                FechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),
                FechaFin = v.FechaFin.ToString("dd/MM/yyyy"),
                DiasHabiles = v.DiasHabiles,
                DiasSolicitados = v.DiasSolicitados,
                Estado = v.Estado.ToString(),
                FechaSolicitud = v.FechaSolicitud.ToString("dd/MM/yyyy"),
                AprobadoPor = v.AprobadoPor
            })
            .ToListAsync();

        return new DataTablesResponse<VacacionListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    public async Task<VacacionViewModel?> GetByIdAsync(int id)
    {
        var v = await _repo.GetByIdWithRelationsAsync(id);
        if (v == null) return null;
        return new VacacionViewModel
        {
            Id = v.Id,
            EmpleadoId = v.EmpleadoId,
            FechaInicio = v.FechaInicio,
            FechaFin = v.FechaFin,
            Observacion = v.Observacion,
            Estado = v.Estado
        };
    }

    public async Task<(bool success, string message, int id)> CreateAsync(VacacionViewModel vm, string creadoPor)
    {
        var diasHabiles = CalcularDiasHabiles(vm.FechaInicio, vm.FechaFin);
        var diasSolicitados = (vm.FechaFin - vm.FechaInicio).Days + 1;
        var diasUsados = await _repo.GetDiasUsadosEnAnioAsync(vm.EmpleadoId, vm.FechaInicio.Year);
        var diasDisponibles = DIAS_VACACIONES_GUATEMALA - diasUsados;

        if (diasHabiles > diasDisponibles)
            return (false, $"El empleado solo tiene {diasDisponibles} días hábiles disponibles.", 0);

        // Verificar conflicto con otra solicitud del mismo período
        var conflicto = await _context.Vacaciones
            .AnyAsync(v => !v.IsDeleted
                && v.EmpleadoId == vm.EmpleadoId
                && v.Estado != EstadoVacacion.Rechazado
                && v.Estado != EstadoVacacion.Cancelado
                && v.FechaInicio <= vm.FechaFin
                && v.FechaFin >= vm.FechaInicio);

        if (conflicto)
            return (false, "El empleado ya tiene una solicitud en ese período.", 0);

        var vacacion = new Vacacion
        {
            EmpleadoId = vm.EmpleadoId,
            FechaInicio = vm.FechaInicio,
            FechaFin = vm.FechaFin,
            DiasHabiles = diasHabiles,
            DiasSolicitados = diasSolicitados,
            Estado = vm.Estado,
            Observacion = vm.Observacion,
            FechaSolicitud = DateTime.Today,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (vm.Estado == EstadoVacacion.Aprobado)
        {
            vacacion.AprobadoPor = creadoPor;
            vacacion.FechaAprobacion = DateTime.Now;
        }

        await _repo.AddAsync(vacacion);
        await _uow.SaveChangesAsync();
        return (true, "Solicitud de vacaciones registrada correctamente.", vacacion.Id);
    }

    public async Task<(bool success, string message)> UpdateAsync(int id, VacacionViewModel vm)
    {
        var vacacion = await _repo.GetByIdAsync(id);
        if (vacacion == null) return (false, "Solicitud no encontrada.");
        if (vacacion.Estado == EstadoVacacion.Aprobado)
            return (false, "No se puede editar una solicitud ya aprobada.");

        vacacion.FechaInicio = vm.FechaInicio;
        vacacion.FechaFin = vm.FechaFin;
        vacacion.DiasHabiles = CalcularDiasHabiles(vm.FechaInicio, vm.FechaFin);
        vacacion.DiasSolicitados = (vm.FechaFin - vm.FechaInicio).Days + 1;
        vacacion.Observacion = vm.Observacion;
        vacacion.UpdatedAt = DateTime.UtcNow;

        _repo.Update(vacacion);
        await _uow.SaveChangesAsync();
        return (true, "Solicitud actualizada correctamente.");
    }

    public async Task<(bool success, string message)> AprobarAsync(int id, string aprobadoPor)
    {
        var vacacion = await _repo.GetByIdAsync(id);
        if (vacacion == null) return (false, "Solicitud no encontrada.");
        if (vacacion.Estado != EstadoVacacion.Pendiente)
            return (false, "Solo se pueden aprobar solicitudes pendientes.");

        vacacion.Estado = EstadoVacacion.Aprobado;
        vacacion.AprobadoPor = aprobadoPor;
        vacacion.FechaAprobacion = DateTime.Now;
        vacacion.UpdatedAt = DateTime.UtcNow;

        _repo.Update(vacacion);
        await _uow.SaveChangesAsync();
        return (true, "Solicitud aprobada correctamente.");
    }

    public async Task<(bool success, string message)> RechazarAsync(int id, string motivo)
    {
        var vacacion = await _repo.GetByIdAsync(id);
        if (vacacion == null) return (false, "Solicitud no encontrada.");

        vacacion.Estado = EstadoVacacion.Rechazado;
        vacacion.Observacion = motivo;
        vacacion.UpdatedAt = DateTime.UtcNow;

        _repo.Update(vacacion);
        await _uow.SaveChangesAsync();
        return (true, "Solicitud rechazada.");
    }

    public async Task<(bool success, string message)> DeleteAsync(int id)
    {
        var vacacion = await _repo.GetByIdAsync(id);
        if (vacacion == null) return (false, "Solicitud no encontrada.");

        vacacion.IsDeleted = true;
        vacacion.UpdatedAt = DateTime.UtcNow;

        _repo.Update(vacacion);
        await _uow.SaveChangesAsync();
        return (true, "Solicitud eliminada correctamente.");
    }

    public async Task<int> GetDiasDisponiblesAsync(int empleadoId, int anio)
    {
        var usados = await _repo.GetDiasUsadosEnAnioAsync(empleadoId, anio);
        return DIAS_VACACIONES_GUATEMALA - usados;
    }

    // ══════════════════════════════════════════
    // AUSENCIAS
    // ══════════════════════════════════════════

    public async Task<List<AusenciaListViewModel>> GetAusenciasAsync()
    {
        return await _context.Ausencias
            .Include(a => a.Empleado).ThenInclude(e => e.Departamento)
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.FechaInicio)
            .Select(a => new AusenciaListViewModel
            {
                Id = a.Id,
                NombreEmpleado = $"{a.Empleado.PrimerNombre} {a.Empleado.PrimerApellido}".Trim(),
                Departamento = a.Empleado.Departamento.Nombre,
                Tipo = a.Tipo.ToString(),
                FechaInicio = a.FechaInicio.ToString("dd/MM/yyyy"),
                FechaFin = a.FechaFin.ToString("dd/MM/yyyy"),
                TotalDias = a.TotalDias,
                Justificada = a.Justificada,
                Observacion = a.Observacion
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> CreateAusenciaAsync(AusenciaViewModel vm)
    {
        var ausencia = new Ausencia
        {
            EmpleadoId = vm.EmpleadoId,
            Tipo = vm.Tipo,
            FechaInicio = vm.FechaInicio,
            FechaFin = vm.FechaFin,
            TotalDias = (vm.FechaFin - vm.FechaInicio).Days + 1,
            Justificada = vm.Justificada,
            Observacion = vm.Observacion,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Ausencias.Add(ausencia);
        await _uow.SaveChangesAsync();
        return (true, "Ausencia registrada correctamente.");
    }

    public async Task<(bool success, string message)> DeleteAusenciaAsync(int id)
    {
        var ausencia = await _context.Ausencias.FindAsync(id);
        if (ausencia == null) return (false, "Ausencia no encontrada.");

        ausencia.IsDeleted = true;
        ausencia.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, "Ausencia eliminada correctamente.");
    }

    // ══════════════════════════════════════════
    // SALDOS
    // ══════════════════════════════════════════

    public async Task<List<SaldoVacacionViewModel>> GetSaldosAsync()
    {
        var anio = DateTime.Now.Year;

        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var vacaciones = await _context.Vacaciones
            .Where(v => !v.IsDeleted && v.Estado == EstadoVacacion.Aprobado)
            .ToListAsync();

        return empleados.Select(e =>
        {
            var aniosCompletos = (int)((DateTime.Today - e.FechaIngreso).TotalDays / 365);
            var diasCorresponden = DIAS_VACACIONES_GUATEMALA;

            var diasTomados = vacaciones
                .Where(v => v.EmpleadoId == e.Id && v.FechaInicio.Year == anio)
                .Sum(v => v.DiasHabiles);

            var diasPendientes = vacaciones
                .Where(v => v.EmpleadoId == e.Id
                         && v.FechaInicio >= DateTime.Today
                         && v.FechaInicio.Year == anio)
                .Sum(v => v.DiasHabiles);

            return new SaldoVacacionViewModel
            {
                EmpleadoId = e.Id,
                NombreEmpleado = $"{e.PrimerNombre} {e.PrimerApellido}".Trim(),
                Departamento = e.Departamento?.Nombre ?? "—",
                Antiguedad = $"{aniosCompletos} año(s)",
                DiasCorresponden = diasCorresponden,
                DiasTomados = diasTomados,
                DiasPendientes = diasPendientes,
                DiasDisponibles = diasCorresponden - diasTomados
            };
        }).ToList();
    }

    // ══════════════════════════════════════════
    // KPIs
    // ══════════════════════════════════════════

    public async Task<VacacionKpiViewModel> GetKPIsAsync()
    {
        var anio = DateTime.Now.Year;
        var hoy = DateTime.Today;

        var solicitudes = await _context.Vacaciones
            .Where(v => !v.IsDeleted && v.FechaSolicitud.Year == anio)
            .ToListAsync();

        return new VacacionKpiViewModel
        {
            TotalSolicitudes = solicitudes.Count,
            Pendientes = solicitudes.Count(v => v.Estado == EstadoVacacion.Pendiente),
            Aprobadas = solicitudes.Count(v => v.Estado == EstadoVacacion.Aprobado),
            EnVacacionesHoy = solicitudes.Count(v =>
                v.Estado == EstadoVacacion.Aprobado &&
                v.FechaInicio <= hoy && v.FechaFin >= hoy)
        };
    }

    // ══════════════════════════════════════════
    // PRIVADOS — Cálculo de días por rango calendario
    // ══════════════════════════════════════════

    private static int CalcularDiasHabiles(DateTime inicio, DateTime fin)
    {
        // Regla solicitada: contar todos los días del rango (inclusive)
        // según fecha inicio/fin capturada en el módulo.
        return Math.Max(0, (fin.Date - inicio.Date).Days + 1);
    }

    private static bool EsFeriado(DateTime fecha)
    {
        return FeriadosGuatemala.Any(f => f.mes == fecha.Month && f.dia == fecha.Day);
    }
}