using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class PrestacionService : IPrestacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IPrestacionRepository _repo;
    private readonly IUnitOfWork _uow;

    public PrestacionService(
        ApplicationDbContext context,
        IPrestacionRepository repo,
        IUnitOfWork uow)
    {
        _context = context;
        _repo = repo;
        _uow = uow;
    }

    // ══════════════════════════════════════════
    // DATATABLES
    // ══════════════════════════════════════════
    public async Task<DataTablesResponse<PrestacionListViewModel>> GetDataTablesAsync(
        DataTablesRequest req)
    {
        var query = _context.Prestaciones
            .Include(p => p.Empleado).ThenInclude(e => e.Departamento)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(p =>
                p.Empleado.PrimerNombre.ToLower().Contains(s) ||
                p.Empleado.PrimerApellido.ToLower().Contains(s) ||
                p.Empleado.Codigo.ToLower().Contains(s));
        }

        if (req.DepartamentoId.HasValue)
            query = query.Where(p => p.Empleado.DepartamentoId == req.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EstadoPrestacion>(req.Estado, out var estado))
            query = query.Where(p => p.Estado == estado);

        if (!string.IsNullOrWhiteSpace(req.TipoPrestacion) &&
            Enum.TryParse<TipoPrestacion>(req.TipoPrestacion, out var tipo))
            query = query.Where(p => p.Tipo == tipo);

        var total = await query.CountAsync();
        query = query.OrderByDescending(p => p.Periodo)
                     .ThenBy(p => p.Empleado.PrimerApellido);

        var data = await query
            .Skip(req.Start).Take(req.Length)
            .Select(p => new PrestacionListViewModel
            {
                Id = p.Id,
                NombreEmpleado = $"{p.Empleado.PrimerNombre} {p.Empleado.PrimerApellido}".Trim(),
                Iniciales = $"{p.Empleado.PrimerNombre[0]}{p.Empleado.PrimerApellido[0]}".ToUpper(),
                FotoUrl = p.Empleado.FotoUrl,
                Departamento = p.Empleado.Departamento.Nombre,
                Tipo = p.Tipo.ToString(),
                Periodo = p.Periodo,
                MesesTrabajados = p.MesesTrabajados,
                SalarioBase = p.SalarioBase,
                Monto = p.Monto,
                Estado = p.Estado.ToString(),
                FechaPago = p.FechaPago.HasValue
                    ? p.FechaPago.Value.ToString("dd/MM/yyyy") : null
            })
            .ToListAsync();

        return new DataTablesResponse<PrestacionListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    // ══════════════════════════════════════════
    // GET BY ID
    // ══════════════════════════════════════════
    public async Task<PrestacionViewModel?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdWithRelationsAsync(id);
        if (p == null) return null;
        return new PrestacionViewModel
        {
            Id = p.Id,
            EmpleadoId = p.EmpleadoId,
            Tipo = p.Tipo,
            Periodo = p.Periodo,
            MesesTrabajados = p.MesesTrabajados,
            SalarioBase = p.SalarioBase,
            Monto = p.Monto,
            Estado = p.Estado,
            FechaPago = p.FechaPago,
            Observacion = p.Observacion
        };
    }

    // ══════════════════════════════════════════
    // CREATE
    // ══════════════════════════════════════════
    public async Task<(bool success, string message, int id)> CreateAsync(
        PrestacionViewModel vm, string calculadoPor)
    {
        if (await _repo.ExistePrestacionAsync(vm.EmpleadoId, vm.Tipo, vm.Periodo))
            return (false,
                $"Ya existe {vm.Tipo} para este empleado en {vm.Periodo}.", 0);

        var prestacion = new Prestacion
        {
            EmpleadoId = vm.EmpleadoId,
            Tipo = vm.Tipo,
            Periodo = vm.Periodo,
            MesesTrabajados = vm.MesesTrabajados,
            SalarioBase = vm.SalarioBase,
            Monto = vm.Monto,
            Estado = vm.Estado,
            FechaPago = vm.FechaPago,
            Observacion = vm.Observacion,
            CalculadoPor = calculadoPor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(prestacion);
        await _uow.SaveChangesAsync();
        return (true, $"{vm.Tipo} registrado correctamente.", prestacion.Id);
    }

    // ══════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════
    public async Task<(bool success, string message)> UpdateAsync(
        int id, PrestacionViewModel vm)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p == null) return (false, "Registro no encontrado.");
        if (p.Estado == EstadoPrestacion.Pagado)
            return (false, "No se puede editar una prestación ya pagada.");

        p.Tipo = vm.Tipo;
        p.Periodo = vm.Periodo;
        p.MesesTrabajados = vm.MesesTrabajados;
        p.SalarioBase = vm.SalarioBase;
        p.Monto = vm.Monto;
        p.Estado = vm.Estado;
        p.FechaPago = vm.FechaPago;
        p.Observacion = vm.Observacion;
        p.UpdatedAt = DateTime.UtcNow;

        _repo.Update(p);
        await _uow.SaveChangesAsync();
        return (true, "Prestación actualizada correctamente.");
    }

    // ══════════════════════════════════════════
    // MARCAR PAGADO
    // ══════════════════════════════════════════
    public async Task<(bool success, string message)> MarcarPagadoAsync(
        int id, DateTime fechaPago)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p == null) return (false, "Registro no encontrado.");

        p.Estado = EstadoPrestacion.Pagado;
        p.FechaPago = fechaPago;
        p.UpdatedAt = DateTime.UtcNow;

        _repo.Update(p);
        await _uow.SaveChangesAsync();
        return (true, "Prestación marcada como pagada.");
    }

    // ══════════════════════════════════════════
    // DELETE
    // ══════════════════════════════════════════
    public async Task<(bool success, string message)> DeleteAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p == null) return (false, "Registro no encontrado.");
        if (p.Estado == EstadoPrestacion.Pagado)
            return (false, "No se puede eliminar una prestación ya pagada.");

        p.IsDeleted = true;
        p.UpdatedAt = DateTime.UtcNow;
        _repo.Update(p);
        await _uow.SaveChangesAsync();
        return (true, "Registro eliminado correctamente.");
    }

    // ══════════════════════════════════════════
    // CALCULAR PRESTACIONES — LÓGICA INTELIGENTE
    // ══════════════════════════════════════════
    public async Task<CalculoPrestacionViewModel?> CalcularPrestacionesAsync(
     int empleadoId, int anio = 0)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Departamento)
            .FirstOrDefaultAsync(e => e.Id == empleadoId && !e.IsDeleted);
        if (empleado == null) return null;

        var hoy = DateTime.Today;
        if (anio == 0) anio = hoy.Year;

        // ══════════════════════════════════════════
        // PERÍODOS LEGALES GUATEMALA
        // Aguinaldo: 01/dic año anterior → 30/nov año actual
        // Bono 14:   01/jul año anterior → 30/jun año actual
        // ══════════════════════════════════════════
        var inicioAguinaldo = new DateTime(anio - 1, 12, 1);
        var finAguinaldo = new DateTime(anio, 11, 30);

        var inicioBono14 = new DateTime(anio - 1, 7, 1);
        var finBono14 = new DateTime(anio, 6, 30);

        // Si el empleado ingresó después del inicio del período, usar su fecha de ingreso
        if (empleado.FechaIngreso > inicioAguinaldo) inicioAguinaldo = empleado.FechaIngreso;
        if (empleado.FechaIngreso > inicioBono14) inicioBono14 = empleado.FechaIngreso;

        // Si el empleado ya salió, limitar al fin
        var fechaSalida = empleado.FechaSalida.HasValue && empleado.FechaSalida < hoy
            ? empleado.FechaSalida.Value : hoy;

        if (fechaSalida < finAguinaldo) finAguinaldo = fechaSalida;
        if (fechaSalida < finBono14) finBono14 = fechaSalida;

        // ── Días calendario en cada período ──
        var diasAguinaldo = Math.Max(0, (finAguinaldo - inicioAguinaldo).Days + 1);
        var diasBono14 = Math.Max(0, (finBono14 - inicioBono14).Days + 1);

        // ── Meses trabajados en cada período (para mostrar) ──
        var mesesAguinaldo = diasAguinaldo > 0
            ? Math.Max(1, (int)Math.Floor(diasAguinaldo / 30.0))
            : 0;
        var mesesBono14 = diasBono14 > 0
            ? Math.Max(1, (int)Math.Floor(diasBono14 / 30.0))
            : 0;

        // ── Vacaciones tomadas en el período de Aguinaldo ──
        var vacAguinaldo = await _context.Vacaciones
            .Where(v => !v.IsDeleted
                     && v.EmpleadoId == empleadoId
                     && v.Estado == EstadoVacacion.Aprobado
                     && v.FechaInicio >= inicioAguinaldo
                     && v.FechaInicio <= finAguinaldo)
            .SumAsync(v => v.DiasHabiles);

        var vacBono14 = await _context.Vacaciones
            .Where(v => !v.IsDeleted
                     && v.EmpleadoId == empleadoId
                     && v.Estado == EstadoVacacion.Aprobado
                     && v.FechaInicio >= inicioBono14
                     && v.FechaInicio <= finBono14)
            .SumAsync(v => v.DiasHabiles);

        // ── Ausencias injustificadas en cada período ──
        var ausAguinaldo = await _context.Ausencias
            .Where(a => !a.IsDeleted
                     && a.EmpleadoId == empleadoId
                     && !a.Justificada
                     && a.FechaInicio >= inicioAguinaldo
                     && a.FechaInicio <= finAguinaldo)
            .SumAsync(a => a.TotalDias);

        var ausBono14 = await _context.Ausencias
            .Where(a => !a.IsDeleted
                     && a.EmpleadoId == empleadoId
                     && !a.Justificada
                     && a.FechaInicio >= inicioBono14
                     && a.FechaInicio <= finBono14)
            .SumAsync(a => a.TotalDias);

        // ── Días efectivos descontando vacaciones y ausencias ──
        var diasEfectivosAguinaldo = Math.Max(0, diasAguinaldo - vacAguinaldo - ausAguinaldo);
        var diasEfectivosBono14 = Math.Max(0, diasBono14 - vacBono14 - ausBono14);

        // ── Salario promedio ponderado si hubo cambios de salario ──
        var historial = await _context.HistorialSalarios
            .Where(h => !h.IsDeleted && h.EmpleadoId == empleadoId)
            .OrderBy(h => h.FechaCambio)
            .ToListAsync();

        var salarioPromedioAguinaldo = CalcularSalarioPonderado(
            empleado.SalarioBase, historial, inicioAguinaldo, finAguinaldo, diasAguinaldo);

        var salarioPromedioBono14 = CalcularSalarioPonderado(
            empleado.SalarioBase, historial, inicioBono14, finBono14, diasBono14);

        // ── Cálculo final ──
        // Aguinaldo = (salario / 365) * días efectivos del período
        var aguinaldo = Math.Round((salarioPromedioAguinaldo / 365m) * diasEfectivosAguinaldo, 2);
        var bono14 = Math.Round((salarioPromedioBono14 / 365m) * diasEfectivosBono14, 2);

        // Indemnización = salario mensual * años TOTALES trabajados (esto sí es total)
        var mesesTotales = ((hoy.Year - empleado.FechaIngreso.Year) * 12)
                            + hoy.Month - empleado.FechaIngreso.Month;
        var aniosTrabajados = mesesTotales / 12;
        var indemnizacion = Math.Round(empleado.SalarioBase * aniosTrabajados, 2);
        var totalFiniquito = aguinaldo + bono14 + indemnizacion;

        return new CalculoPrestacionViewModel
        {
            EmpleadoId = empleado.Id,
            NombreEmpleado = empleado.NombreCompleto,
            Departamento = empleado.Departamento?.Nombre ?? "—",
            SalarioBase = empleado.SalarioBase,
            SalarioPromedio = Math.Round(salarioPromedioAguinaldo, 2),
            FechaIngreso = empleado.FechaIngreso,
            MesesTrabajados = mesesAguinaldo,
            AniosTrabajados = aniosTrabajados,
            DiasEfectivos = diasEfectivosAguinaldo,
            DiasVacacionesTomados = vacAguinaldo,
            DiasAusencias = ausAguinaldo,
            HuboambioSalario = historial.Any(),
            Aguinaldo = aguinaldo,
            Bono14 = bono14,
            Indemnizacion = indemnizacion,
            TotalFiniquito = Math.Round(totalFiniquito, 2)
        };
    }

    public async Task<(bool success, string message, int cantidad)>
        GenerarPrestacionesAnioAsync(int anio, string calculadoPor, int? departamentoId = null)
    {
        var empleadosQuery = _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .AsQueryable();

        if (departamentoId.HasValue)
            empleadosQuery = empleadosQuery.Where(e => e.DepartamentoId == departamentoId.Value);

        var empleados = await empleadosQuery.ToListAsync();

        int generados = 0;

        // ── Períodos legales Guatemala ──
        var inicioAguinaldo = new DateTime(anio - 1, 12, 1);
        var finAguinaldo = new DateTime(anio, 11, 30);
        var inicioBono14 = new DateTime(anio - 1, 7, 1);
        var finBono14 = new DateTime(anio, 6, 30);

        foreach (var emp in empleados)
        {
            // Calcular con la lógica correcta por período
            var calculo = await CalcularPrestacionesAsync(emp.Id);
            if (calculo == null) continue;

            // ── Meses reales en el período de Aguinaldo ──
            var inicioAguEmp = emp.FechaIngreso > inicioAguinaldo
                ? emp.FechaIngreso : inicioAguinaldo;
            var mesesAgu = Math.Max(0,
                ((finAguinaldo.Year - inicioAguEmp.Year) * 12)
                + finAguinaldo.Month - inicioAguEmp.Month);

            // ── Meses reales en el período de Bono14 ──
            var inicioBonEmp = emp.FechaIngreso > inicioBono14
                ? emp.FechaIngreso : inicioBono14;
            var mesesBon = Math.Max(0,
                ((finBono14.Year - inicioBonEmp.Year) * 12)
                + finBono14.Month - inicioBonEmp.Month);

            // Aguinaldo
            if (!await _repo.ExistePrestacionAsync(emp.Id, TipoPrestacion.Aguinaldo, anio))
            {
                await _repo.AddAsync(new Prestacion
                {
                    EmpleadoId = emp.Id,
                    Tipo = TipoPrestacion.Aguinaldo,
                    Periodo = anio,
                    MesesTrabajados = mesesAgu,
                    SalarioBase = calculo.SalarioPromedio,
                    Monto = calculo.Aguinaldo,
                    Estado = EstadoPrestacion.Calculado,
                    Observacion = $"Período: 01/dic/{anio - 1} al 30/nov/{anio}. " +
                                      $"Días efectivos: {calculo.DiasEfectivos}" +
                                      (calculo.DiasVacacionesTomados > 0
                                        ? $" (vac: -{calculo.DiasVacacionesTomados})" : "") +
                                      (calculo.DiasAusencias > 0
                                        ? $" (aus: -{calculo.DiasAusencias})" : "") +
                                      (calculo.HuboambioSalario
                                        ? $". Salario promedio: Q{calculo.SalarioPromedio:N2}" : ""),
                    CalculadoPor = calculadoPor,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                generados++;
            }

            // Bono 14
            if (!await _repo.ExistePrestacionAsync(emp.Id, TipoPrestacion.Bono14, anio))
            {
                await _repo.AddAsync(new Prestacion
                {
                    EmpleadoId = emp.Id,
                    Tipo = TipoPrestacion.Bono14,
                    Periodo = anio,
                    MesesTrabajados = mesesBon,
                    SalarioBase = calculo.SalarioPromedio,
                    Monto = calculo.Bono14,
                    Estado = EstadoPrestacion.Calculado,
                    Observacion = $"Período: 01/jul/{anio - 1} al 30/jun/{anio}. " +
                                      $"Días efectivos: {calculo.DiasEfectivos}" +
                                      (calculo.DiasVacacionesTomados > 0
                                        ? $" (vac: -{calculo.DiasVacacionesTomados})" : "") +
                                      (calculo.DiasAusencias > 0
                                        ? $" (aus: -{calculo.DiasAusencias})" : "") +
                                      (calculo.HuboambioSalario
                                        ? $". Salario promedio: Q{calculo.SalarioPromedio:N2}" : ""),
                    CalculadoPor = calculadoPor,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                generados++;
            }
        }

        await _uow.SaveChangesAsync();
        var alcance = departamentoId.HasValue
            ? "del departamento seleccionado"
            : "de todos los departamentos";

        return (true,
            $"Se generaron {generados} registros ({alcance}) con períodos legales de Guatemala.",
            generados);
    }

    // ── Método privado: salario promedio ponderado por período ──
    private static decimal CalcularSalarioPonderado(
        decimal salarioActual,
        List<HistorialSalario> historial,
        DateTime inicioPeriodo,
        DateTime finPeriodo,
        int diasTotales)
    {
        if (diasTotales <= 0) return salarioActual;

        // Filtrar solo cambios dentro del período
        var cambiosEnPeriodo = historial
            .Where(h => h.FechaCambio >= inicioPeriodo && h.FechaCambio <= finPeriodo)
            .OrderBy(h => h.FechaCambio)
            .ToList();

        if (!cambiosEnPeriodo.Any()) return salarioActual;

        decimal totalPonderado = 0;
        var fechaRef = inicioPeriodo;
        var salarioRef = cambiosEnPeriodo.First().SalarioAnterior;

        foreach (var cambio in cambiosEnPeriodo)
        {
            var dias = (cambio.FechaCambio - fechaRef).Days;
            totalPonderado += salarioRef * dias;
            fechaRef = cambio.FechaCambio;
            salarioRef = cambio.SalarioNuevo;
        }

        // Último tramo hasta fin del período
        var diasFinales = (finPeriodo - fechaRef).Days + 1;
        totalPonderado += salarioActual * diasFinales;

        return totalPonderado / diasTotales;
    }
}