using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class AsistenciaService : IAsistenciaService
{
    private readonly ApplicationDbContext _context;

    public AsistenciaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers de zona horaria Guatemala (UTC-6)
    // ─────────────────────────────────────────────────────────────
    private static readonly TimeZoneInfo _zonaGT =
        TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

    private static DateTime HoyGuatemala() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaGT).Date;

    // ✅ FIX — maneja los 3 casos posibles de Kind
    // línea 29 — queda así, sin nada abajo
    private static DateTime FechaGuatemala(DateTime fecha) => fecha.Date;

   
    // ─────────────────────────────────────────────────────────────
    //  KPIs del día
    // ─────────────────────────────────────────────────────────────
    public async Task<AsistenciaKpiViewModel> GetKpisHoyAsync()
    {
        var hoy = HoyGuatemala();

        var registrosHoy = await _context.Asistencias
            .Where(a => !a.IsDeleted && a.Fecha.Date == hoy)
            .ToListAsync();

        var totalEmpleados = await _context.Empleados
            .CountAsync(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        var presentes = registrosHoy.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
        var tardanzas = registrosHoy.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        var hExtra = registrosHoy.Sum(a => a.HorasExtra);

        return new AsistenciaKpiViewModel
        {
            Presentes = presentes,
            Ausentes = totalEmpleados - presentes,
            Tardanzas = tardanzas,
            HorasExtra = hExtra,
            TotalEmpleados = totalEmpleados
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  DataTables – Registros de asistencia
    // ─────────────────────────────────────────────────────────────
    public async Task<DataTablesResponse<AsistenciaListViewModel>> GetDataTablesAsync(DataTablesRequest request)
    {
        var query = _context.Asistencias
            .Where(a => !a.IsDeleted)
            .Include(a => a.Empleado).ThenInclude(e => e.Departamento)
            .AsQueryable();

        if (request.DepartamentoId.HasValue && request.DepartamentoId > 0)
            query = query.Where(a => a.Empleado.DepartamentoId == request.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(request.Estado) &&
            Enum.TryParse<EstadoAsistencia>(request.Estado, out var estadoEnum))
            query = query.Where(a => a.Estado == estadoEnum);

        if (DateTime.TryParse(request.FechaDesde, out var desde))
            query = query.Where(a => a.Fecha.Date >= desde.Date);

        if (DateTime.TryParse(request.FechaHasta, out var hasta))
            query = query.Where(a => a.Fecha.Date <= hasta.Date);

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var search = request.SearchValue.ToLower();
            query = query.Where(a =>
                a.Empleado.PrimerNombre.ToLower().Contains(search) ||
                a.Empleado.PrimerApellido.ToLower().Contains(search) ||
                (a.Empleado.Departamento != null && a.Empleado.Departamento.Nombre.ToLower().Contains(search)));
        }

        var total = await query.CountAsync();

        query = (request.OrderColumn?.ToLower(), request.OrderDir?.ToLower()) switch
        {
            ("fecha", "asc") => query.OrderBy(a => a.Fecha),
            ("fecha", _) => query.OrderByDescending(a => a.Fecha),
            ("empleado", "asc") => query.OrderBy(a => a.Empleado.PrimerApellido),
            ("empleado", _) => query.OrderByDescending(a => a.Empleado.PrimerApellido),
            _ => query.OrderByDescending(a => a.Fecha).ThenBy(a => a.Empleado.PrimerApellido)
        };

        var datos = await query
            .Skip(request.Start)
            .Take(request.Length)
            .Select(a => new AsistenciaListViewModel
            {
                Id = a.Id,
                EmpleadoId = a.EmpleadoId,
                NombreEmpleado = $"{a.Empleado.PrimerNombre} {a.Empleado.PrimerApellido}",
                Iniciales = $"{a.Empleado.PrimerNombre[0]}{a.Empleado.PrimerApellido[0]}".ToUpper(),
                FotoUrl = a.Empleado.FotoUrl,
                Departamento = a.Empleado.Departamento != null ? a.Empleado.Departamento.Nombre : "—",
                Fecha = a.Fecha.ToString("dd/MM/yyyy"),
                HoraEntrada = a.HoraEntrada.HasValue ? a.HoraEntrada.Value.ToString(@"hh\:mm") : null,
                HoraSalida = a.HoraSalida.HasValue ? a.HoraSalida.Value.ToString(@"hh\:mm") : null,
                HorasExtra = a.HorasExtra,
                MinutosAtraso = a.MinutosAtraso,
                Metodo = a.Metodo.ToString(),
                Estado = a.Estado.ToString(),
                HorasTrabajadas = a.HoraEntrada.HasValue && a.HoraSalida.HasValue
                    ? (decimal)(a.HoraSalida.Value >= a.HoraEntrada.Value
                        ? (a.HoraSalida.Value - a.HoraEntrada.Value).TotalHours
                        : (a.HoraSalida.Value - a.HoraEntrada.Value + TimeSpan.FromHours(24)).TotalHours)
                    : 0
            })
            .ToListAsync();

        return new DataTablesResponse<AsistenciaListViewModel>
        {
            Draw = request.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = datos
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  CRUD – Asistencia
    // ─────────────────────────────────────────────────────────────
    public async Task<AsistenciaViewModel?> GetByIdAsync(int id)
    {
        var a = await _context.Asistencias
            .Include(x => x.Empleado)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return null;

        return new AsistenciaViewModel
        {
            Id = a.Id,
            EmpleadoId = a.EmpleadoId,
            NombreEmpleado = $"{a.Empleado.PrimerNombre} {a.Empleado.PrimerApellido}",
            HorarioId = a.HorarioId,
            Fecha = a.Fecha,
            HoraEntrada = a.HoraEntrada?.ToString(@"hh\:mm"),
            HoraSalida = a.HoraSalida?.ToString(@"hh\:mm"),
            HorasExtra = a.HorasExtra,
            MinutosAtraso = a.MinutosAtraso,
            Metodo = a.Metodo,
            Estado = a.Estado,
            Observacion = a.Observacion
        };
    }

    public async Task<(bool, string, int)> CreateAsync(AsistenciaViewModel vm)
    {
        // ✅ FIX UTC — convierte la fecha recibida a fecha Guatemala antes de comparar
        var fechaGT = FechaGuatemala(vm.Fecha);

        var existe = await _context.Asistencias
            .AnyAsync(a => !a.IsDeleted && a.EmpleadoId == vm.EmpleadoId && a.Fecha.Date == fechaGT);

        if (existe)
            return (false, "Ya existe un registro de asistencia para este empleado en esa fecha.", 0);

        var entrada = ParseTime(vm.HoraEntrada);
        var salida = ParseTime(vm.HoraSalida);

        int minutosAtraso = vm.MinutosAtraso;
        decimal horasExtra = vm.HorasExtra;
        var estadoFinal = vm.Estado;

        if (vm.HorarioId.HasValue && entrada.HasValue)
        {
            var horario = await _context.Horarios.FindAsync(vm.HorarioId.Value);
            if (horario != null)
            {
                var limiteEntrada = horario.HoraEntrada.Add(TimeSpan.FromMinutes(horario.MinutosToleranciaTardanza));

                if (entrada.Value > limiteEntrada)
                {
                    minutosAtraso = (int)(entrada.Value - horario.HoraEntrada).TotalMinutes;
                    estadoFinal = EstadoAsistencia.Tardanza;
                }

                if (salida.HasValue)
                {
                    var diffSalida = salida.Value - horario.HoraSalida;
                    if (diffSalida.TotalMinutes > 0)
                        horasExtra = (decimal)diffSalida.TotalHours;
                }
            }
        }

        var entity = new Asistencia
        {
            EmpleadoId = vm.EmpleadoId,
            HorarioId = vm.HorarioId,
            Fecha = fechaGT, // ✅ FIX UTC — guarda fecha Guatemala
            HoraEntrada = entrada,
            HoraSalida = salida,
            HorasExtra = horasExtra,
            MinutosAtraso = minutosAtraso,
            Metodo = vm.Metodo,
            Estado = estadoFinal,
            Observacion = vm.Observacion
        };

        _context.Asistencias.Add(entity);
        await _context.SaveChangesAsync();
        return (true, "Asistencia registrada correctamente.", entity.Id);
    }

    public async Task<(bool, string)> UpdateAsync(int id, AsistenciaViewModel vm)
    {
        var entity = await _context.Asistencias.FindAsync(id);
        if (entity == null) return (false, "Registro no encontrado.");

        // ✅ FIX UTC — convierte la fecha recibida a fecha Guatemala antes de comparar
        var fechaGT = FechaGuatemala(vm.Fecha);

        var duplicado = await _context.Asistencias
            .AnyAsync(a => !a.IsDeleted && a.Id != id && a.EmpleadoId == vm.EmpleadoId && a.Fecha.Date == fechaGT);

        if (duplicado)
            return (false, "Ya existe un registro de asistencia para este empleado en esa fecha.");

        var entrada = ParseTime(vm.HoraEntrada);
        var salida = ParseTime(vm.HoraSalida);

        int minutosAtraso = vm.MinutosAtraso;
        decimal horasExtra = vm.HorasExtra;
        var estadoFinal = vm.Estado;

        if (vm.HorarioId.HasValue && entrada.HasValue)
        {
            var horario = await _context.Horarios.FindAsync(vm.HorarioId.Value);
            if (horario != null)
            {
                var limiteEntrada = horario.HoraEntrada.Add(TimeSpan.FromMinutes(horario.MinutosToleranciaTardanza));

                if (entrada.Value > limiteEntrada)
                {
                    minutosAtraso = (int)(entrada.Value - horario.HoraEntrada).TotalMinutes;
                    estadoFinal = EstadoAsistencia.Tardanza;
                }

                if (salida.HasValue)
                {
                    var diffSalida = salida.Value - horario.HoraSalida;
                    if (diffSalida.TotalHours < -12) diffSalida = diffSalida.Add(TimeSpan.FromHours(24));
                    if (diffSalida.TotalMinutes > 0)
                        horasExtra = (decimal)diffSalida.TotalHours;
                }
            }
        }

        entity.EmpleadoId = vm.EmpleadoId;
        entity.HorarioId = vm.HorarioId;
        entity.Fecha = fechaGT; // ✅ FIX UTC — guarda fecha Guatemala
        entity.HoraEntrada = entrada;
        entity.HoraSalida = salida;
        entity.HorasExtra = horasExtra;
        entity.MinutosAtraso = minutosAtraso;
        entity.Metodo = vm.Metodo;
        entity.Estado = estadoFinal;
        entity.Observacion = vm.Observacion;

        await _context.SaveChangesAsync();
        return (true, "Asistencia actualizada correctamente.");
    }

    public async Task<(bool, string)> DeleteAsync(int id)
    {
        var entity = await _context.Asistencias.FindAsync(id);
        if (entity == null) return (false, "Registro no encontrado.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return (true, "Registro eliminado correctamente.");
    }

    // ─────────────────────────────────────────────────────────────
    //  DataTables – Horarios
    // ─────────────────────────────────────────────────────────────
    public async Task<DataTablesResponse<HorarioListViewModel>> GetHorariosDataTablesAsync(DataTablesRequest request)
    {
        var query = _context.Horarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var s = request.SearchValue.ToLower();
            query = query.Where(h => h.Nombre.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var datos = await query
            .OrderBy(h => h.Nombre)
            .Skip(request.Start)
            .Take(request.Length)
            .Select(h => new HorarioListViewModel
            {
                Id = h.Id,
                Nombre = h.Nombre,
                HoraEntrada = h.HoraEntrada.ToString(@"hh\:mm"),
                HoraSalida = h.HoraSalida.ToString(@"hh\:mm"),
                MinutosToleranciaTardanza = h.MinutosToleranciaTardanza,
                Activo = h.Activo,
                TotalEmpleados = h.Asistencias.Count()
            })
            .ToListAsync();

        return new DataTablesResponse<HorarioListViewModel>
        {
            Draw = request.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = datos
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  CRUD – Horarios
    // ─────────────────────────────────────────────────────────────
    public async Task<HorarioViewModel?> GetHorarioByIdAsync(int id)
    {
        var h = await _context.Horarios.FindAsync(id);
        if (h == null) return null;

        return new HorarioViewModel
        {
            Id = h.Id,
            Nombre = h.Nombre,
            HoraEntrada = h.HoraEntrada.ToString(@"hh\:mm"),
            HoraSalida = h.HoraSalida.ToString(@"hh\:mm"),
            MinutosToleranciaTardanza = h.MinutosToleranciaTardanza,
            Activo = h.Activo
        };
    }

    public async Task<(bool, string, int)> CreateHorarioAsync(HorarioViewModel vm)
    {
        var existe = await _context.Horarios
            .AnyAsync(h => h.Nombre.ToLower() == vm.Nombre.ToLower());

        if (existe)
            return (false, $"Ya existe un horario con el nombre '{vm.Nombre}'.", 0);

        var entity = new Horario
        {
            Nombre = vm.Nombre.Trim(),
            HoraEntrada = TimeSpan.Parse(vm.HoraEntrada),
            HoraSalida = TimeSpan.Parse(vm.HoraSalida),
            MinutosToleranciaTardanza = vm.MinutosToleranciaTardanza,
            Activo = vm.Activo
        };

        _context.Horarios.Add(entity);
        await _context.SaveChangesAsync();
        return (true, "Horario creado correctamente.", entity.Id);
    }

    public async Task<(bool, string)> UpdateHorarioAsync(int id, HorarioViewModel vm)
    {
        var entity = await _context.Horarios.FindAsync(id);
        if (entity == null) return (false, "Horario no encontrado.");

        var duplicado = await _context.Horarios
            .AnyAsync(h => h.Id != id && h.Nombre.ToLower() == vm.Nombre.ToLower());

        if (duplicado)
            return (false, $"Ya existe otro horario con el nombre '{vm.Nombre}'.");

        entity.Nombre = vm.Nombre.Trim();
        entity.HoraEntrada = TimeSpan.Parse(vm.HoraEntrada);
        entity.HoraSalida = TimeSpan.Parse(vm.HoraSalida);
        entity.MinutosToleranciaTardanza = vm.MinutosToleranciaTardanza;
        entity.Activo = vm.Activo;

        await _context.SaveChangesAsync();
        return (true, "Horario actualizado correctamente.");
    }

    public async Task<(bool, string)> DeleteHorarioAsync(int id)
    {
        var entity = await _context.Horarios.FindAsync(id);
        if (entity == null) return (false, "Horario no encontrado.");

        var enUso = await _context.Asistencias.AnyAsync(a => a.HorarioId == id);
        if (enUso)
            return (false, "No se puede eliminar: este horario tiene registros de asistencia asociados.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return (true, "Horario eliminado correctamente.");
    }

    public async Task<(bool, string)> ToggleActivoAsync(int id)
    {
        var entity = await _context.Horarios.FindAsync(id);
        if (entity == null) return (false, "Horario no encontrado.");

        entity.Activo = !entity.Activo;
        await _context.SaveChangesAsync();

        return (true, $"Horario {(entity.Activo ? "activado" : "desactivado")} correctamente.");
    }

    // ─────────────────────────────────────────────────────────────
    //  Utilidad
    // ─────────────────────────────────────────────────────────────
    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeSpan.TryParse(value, out var ts) ? ts : null;
    }
}