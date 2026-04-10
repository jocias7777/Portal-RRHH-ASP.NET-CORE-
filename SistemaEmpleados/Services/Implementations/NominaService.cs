using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services;

public class NominaService : INominaService
{
    private readonly ApplicationDbContext _db;

    // ── Constantes legales Guatemala 2024-2025 ──
    private const decimal IGSS_LABORAL = 0.0483m;
    private const decimal IGSS_PATRONAL = 0.1267m;
    private const decimal BONIFICACION_INCENTIVO = 250m;
    private const decimal VALOR_HORA_EXTRA_FACTOR = 1.5m;
    private const decimal ISR_RENTA_EXENTA_ANUAL = 48000m;
    private const decimal ISR_TASA_1 = 0.05m;
    private const decimal ISR_TASA_2 = 0.07m;
    private const decimal ISR_TRAMO_1_MAX = 300000m;

    public NominaService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ════════════════════════════════════════════
    // GENERAR PLANILLA — lógica principal completa
    // ════════════════════════════════════════════
    public async Task<(bool success, string message, int newId)> GenerarPlanillaAsync(
        int mes, int anio, string generadoPor)
    {
        // 1. Validar que no exista planilla activa
        var existe = await _db.Planillas
            .AnyAsync(p => p.Mes == mes && p.Anio == anio
                        && p.Estado != EstadoPlanilla.Anulada);
        if (existe)
            return (false,
                $"Ya existe una planilla activa para {NombreMes(mes)} {anio}.", 0);

        // 2. Empleados activos
        var empleados = await _db.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Puesto)
            .Where(e => e.Estado == EstadoEmpleado.Activo && !e.IsDeleted)
            .ToListAsync();

        if (!empleados.Any())
            return (false, "No hay empleados activos para generar la planilla.", 0);

        // 3. Asistencias del período
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

        var asistencias = await _db.Asistencias
            .Where(a => a.Fecha >= fechaInicio
                     && a.Fecha <= fechaFin
                     && !a.IsDeleted)
            .ToListAsync();

        // 4. Préstamos activos — descuento automático en planilla
        var prestamosActivos = await _db.PrestamosEmpleado
            .Where(p => p.Estado == EstadoPrestamo.Activo && !p.IsDeleted)
            .ToListAsync();

        // 5. Días hábiles del mes
        int diasHabiles = ContarDiasHabiles(fechaInicio, fechaFin);

        // 6. Indicadores especiales del mes
        bool esMesBonoFourteen = mes == 7;   // Julio   — Bono 14 Dto.42-92
        bool esMesAguinaldo = mes == 12;  // Diciembre — Aguinaldo Dto.76-78

        var planilla = new Planilla
        {
            Mes = mes,
            Anio = anio,
            Periodo = int.Parse($"{anio}{mes:D2}"),
            Estado = EstadoPlanilla.Borrador,
            GeneradoPor = generadoPor,
            FechaGeneracion = DateTime.Now
        };

        var detalles = new List<DetallePlanilla>();

        foreach (var emp in empleados)
        {
            // ── Ausencias injustificadas del mes ──
            var diasAusente = await _db.Ausencias
                .Where(a => !a.IsDeleted
                         && a.EmpleadoId == emp.Id
                         && !a.Justificada
                         && a.FechaInicio.Month == mes
                         && a.FechaInicio.Year == anio)
                .SumAsync(a => a.TotalDias);

            decimal descuentoAusencias =
                (emp.SalarioBase / 30m) * diasAusente;

            // ── Asistencia del empleado ──
            var asistEmp = asistencias
                .Where(a => a.EmpleadoId == emp.Id).ToList();

            decimal totalHorasExtra = asistEmp.Sum(a => a.HorasExtra);
            int totalMinutosAtraso = asistEmp.Sum(a => a.MinutosAtraso);

            decimal descuentoAtraso = CalcularDescuentoAtraso(
                emp.SalarioBase, totalMinutosAtraso, diasHabiles);

            int diasTrabajados = asistEmp
                .Count(a => a.Estado != EstadoAsistencia.Ausente);

            // ── Salario del período ──
            decimal salarioPeriodo = emp.TipoContrato == TipoContrato.Temporal
                ? (emp.SalarioBase / diasHabiles) * diasTrabajados
                : emp.SalarioBase;

            salarioPeriodo -= descuentoAtraso;
            if (salarioPeriodo < 0) salarioPeriodo = 0;

            // ── Horas extra ──
            decimal valorHoraOrdinaria = diasHabiles > 0
                ? emp.SalarioBase / (diasHabiles * 8) : 0;
            decimal montoHorasExtra = totalHorasExtra
                                    * valorHoraOrdinaria
                                    * VALOR_HORA_EXTRA_FACTOR;

            // ── Bono 14 (julio) Decreto 42-92 ──
            decimal bono14 = esMesBonoFourteen
                ? CalcularBono14(emp.SalarioBase, emp.FechaIngreso, anio)
                : 0;

            // ── Aguinaldo (diciembre) Decreto 76-78 ──
            decimal aguinaldo = esMesAguinaldo
                ? CalcularAguinaldo(emp.SalarioBase, emp.FechaIngreso, anio)
                : 0;

            // ── Total devengado ──
            decimal totalDevengado = salarioPeriodo
                                   + montoHorasExtra
                                   + BONIFICACION_INCENTIVO
                                   + bono14
                                   + aguinaldo;

            // ── IGSS laboral 4.83% (cargo empleado) ──
            decimal baseIGSS = salarioPeriodo + montoHorasExtra;

            decimal cuotaIGSSLaboral =
                Math.Round(baseIGSS * IGSS_LABORAL, 2);

            decimal cuotaIGSSPatronal =
                Math.Round(baseIGSS * IGSS_PATRONAL, 2);

            // ── ISR mensual proyectado ──
            decimal isr = CalcularISRMensual(emp.SalarioBase, mes);

            // ── Descuento cuota préstamo activo ──
            decimal descuentoPrestamo = 0;
            var prestamo = prestamosActivos
                .FirstOrDefault(p => p.EmpleadoId == emp.Id);
            if (prestamo != null)
            {
                descuentoPrestamo = Math.Min(
                    prestamo.CuotaMensual,
                    prestamo.SaldoPendiente);
            }

            // ── Total deducciones empleado ──
            decimal totalDeducciones = cuotaIGSSLaboral
                                     + isr
                                     + descuentoAusencias
                                     + descuentoPrestamo;

            decimal salarioNetoFinal = totalDevengado - totalDeducciones;
            if (salarioNetoFinal < 0) salarioNetoFinal = 0;

            // ── Observación automática ──
            var obsPartes = new List<string>();
            if (diasAusente > 0)
                obsPartes.Add(
                    $"{diasAusente} día(s) ausencia injustificada " +
                    $"(Q{descuentoAusencias:N2})");
            if (totalMinutosAtraso > 0)
                obsPartes.Add(
                    $"Atraso {totalMinutosAtraso} min " +
                    $"(Q{descuentoAtraso:N2})");
            if (descuentoPrestamo > 0)
                obsPartes.Add(
                    $"Cuota préstamo Q{descuentoPrestamo:N2}");
            if (bono14 > 0)
                obsPartes.Add($"Bono 14 Q{bono14:N2}");
            if (aguinaldo > 0)
                obsPartes.Add($"Aguinaldo Q{aguinaldo:N2}");

            detalles.Add(new DetallePlanilla
            {
                EmpleadoId = emp.Id,
                SalarioBase = emp.SalarioBase,
                HorasExtraMonto = Math.Round(montoHorasExtra, 2),
                Bonificacion250 = BONIFICACION_INCENTIVO,
                OtrosBonos = Math.Round(bono14 + aguinaldo, 2),
                Bono14 = Math.Round(bono14, 2),
                Aguinaldo = Math.Round(aguinaldo, 2),
                TotalDevengado = Math.Round(totalDevengado, 2),
                CuotaIGSS = cuotaIGSSLaboral,
                CuotaIGSSPatronal = cuotaIGSSPatronal,
                ISR = Math.Round(isr, 2),
                OtrasDeducciones = Math.Round(
                    descuentoAusencias + descuentoPrestamo, 2),
                DescuentoPrestamo = Math.Round(descuentoPrestamo, 2),
                TotalDeducciones = Math.Round(totalDeducciones, 2),
                SalarioNeto = Math.Round(salarioNetoFinal, 2),
                Observacion = obsPartes.Any()
                    ? string.Join(" | ", obsPartes) : null
            });

            // ── Descontar cuota del préstamo automáticamente ──
            if (prestamo != null && descuentoPrestamo > 0)
            {
                prestamo.SaldoPendiente -= descuentoPrestamo;
                prestamo.CuotasPagadas += 1;
                if (prestamo.SaldoPendiente <= 0)
                {
                    prestamo.SaldoPendiente = 0;
                    prestamo.CuotasPagadas = prestamo.NumeroCuotas;
                    prestamo.Estado = EstadoPrestamo.Completado;
                }
                prestamo.UpdatedAt = DateTime.Now;
            }
        }

        // 7. Totales de la planilla
        planilla.TotalDevengado = detalles.Sum(d => d.TotalDevengado);
        planilla.TotalDeducciones = detalles.Sum(d => d.TotalDeducciones);
        planilla.TotalNeto = detalles.Sum(d => d.SalarioNeto);
        planilla.Detalles = detalles;

        _db.Planillas.Add(planilla);
        await _db.SaveChangesAsync();

        // 8. Mensaje con alertas del mes
        var mensajeExtra = new List<string>();
        if (esMesBonoFourteen) mensajeExtra.Add("incluye Bono 14");
        if (esMesAguinaldo) mensajeExtra.Add("incluye Aguinaldo");
        int descuentados = detalles.Count(d => d.DescuentoPrestamo > 0);
        if (descuentados > 0)
            mensajeExtra.Add(
                $"{descuentados} préstamo(s) descontados");

        var mensaje = $"Planilla de {NombreMes(mes)} {anio} " +
                      $"generada con {detalles.Count} empleados" +
                      (mensajeExtra.Any()
                          ? $" ({string.Join(", ", mensajeExtra)})."
                          : ".");

        return (true, mensaje, planilla.Id);
    }

    // ════════════════════════════════════════════
    // BONO 14 — Decreto 42-92
    // Período: 1 julio año anterior → 30 junio año actual
    // ════════════════════════════════════════════
    private static decimal CalcularBono14(
        decimal salarioBase, DateTime fechaIngreso, int anio)
    {
        var inicioPeriodo = new DateTime(anio - 1, 7, 1);
        var finPeriodo = new DateTime(anio, 6, 30);
        var inicioEfectivo = fechaIngreso > inicioPeriodo
            ? fechaIngreso : inicioPeriodo;

        var mesesTrabajados =
            ((finPeriodo.Year - inicioEfectivo.Year) * 12)
            + finPeriodo.Month - inicioEfectivo.Month + 1;

        if (mesesTrabajados <= 0) return 0;
        if (mesesTrabajados >= 12) return salarioBase;

        return Math.Round(salarioBase * mesesTrabajados / 12, 2);
    }

    // ════════════════════════════════════════════
    // AGUINALDO — Decreto 76-78
    // Período: 1 diciembre año anterior → 30 noviembre año actual
    // ════════════════════════════════════════════
    private static decimal CalcularAguinaldo(
        decimal salarioBase, DateTime fechaIngreso, int anio)
    {
        var inicioPeriodo = new DateTime(anio - 1, 12, 1);
        var finPeriodo = new DateTime(anio, 11, 30);
        var inicioEfectivo = fechaIngreso > inicioPeriodo
            ? fechaIngreso : inicioPeriodo;

        var mesesTrabajados =
            ((finPeriodo.Year - inicioEfectivo.Year) * 12)
            + finPeriodo.Month - inicioEfectivo.Month + 1;

        if (mesesTrabajados <= 0) return 0;
        if (mesesTrabajados >= 12) return salarioBase;

        return Math.Round(salarioBase * mesesTrabajados / 12, 2);
    }

    // ════════════════════════════════════════════
    // ISR MENSUAL — ley Guatemala
    // ════════════════════════════════════════════
    private static decimal CalcularISRMensual(
    decimal salarioBase,
    int mesActual)
    {
        // 🔹 Base mensual gravable (NO incluye bono incentivo si decides excluirlo)
        decimal ingresoMensual = salarioBase;

        // 🔹 Proyección anual
        decimal ingresoAnual = ingresoMensual * 12;

        // 🔹 Renta exenta
        decimal baseImponible = ingresoAnual - ISR_RENTA_EXENTA_ANUAL;

        if (baseImponible <= 0)
            return 0;

        decimal isrAnual;

        if (baseImponible <= ISR_TRAMO_1_MAX)
        {
            isrAnual = baseImponible * ISR_TASA_1;
        }
        else
        {
            isrAnual = (ISR_TRAMO_1_MAX * ISR_TASA_1)
                     + ((baseImponible - ISR_TRAMO_1_MAX) * ISR_TASA_2);
        }

        return Math.Round(isrAnual / 12, 2);
    }

    // ════════════════════════════════════════════
    // DESCUENTO POR ATRASO
    // ════════════════════════════════════════════
    private static decimal CalcularDescuentoAtraso(
        decimal salarioBase, int minutosAtraso, int diasHabiles)
    {
        if (minutosAtraso <= 0) return 0;
        int minutosLaboralesMes = diasHabiles * 8 * 60;
        return Math.Round(
            salarioBase * ((decimal)minutosAtraso
                / minutosLaboralesMes), 2);
    }

    // ════════════════════════════════════════════
    // DÍAS HÁBILES DEL MES
    // ════════════════════════════════════════════
    private static int ContarDiasHabiles(DateTime inicio, DateTime fin)
    {
        int dias = 0;
        for (var d = inicio; d <= fin; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday
             && d.DayOfWeek != DayOfWeek.Sunday)
                dias++;
        return dias == 0 ? 22 : dias;
    }

    // ════════════════════════════════════════════
    // DATATABLES
    // ════════════════════════════════════════════
    public async Task<DataTablesResponse> GetDataTablesAsync(
        DataTablesRequest request)
    {
        var query = _db.Planillas
            .Include(p => p.Detalles)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Estado)
            && Enum.TryParse<EstadoPlanilla>(
                request.Estado, out var estadoEnum))
            query = query.Where(p => p.Estado == estadoEnum);

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var s = request.SearchValue.ToLower();
            query = query.Where(p =>
                p.GeneradoPor!.ToLower().Contains(s));
        }

        int total = await query.CountAsync();

        var data = await query
            .OrderByDescending(p => p.Anio)
            .ThenByDescending(p => p.Mes)
            .Skip(request.Start)
            .Take(request.Length)
            .Select(p => new PlanillaListViewModel
            {
                Id = p.Id,
                Periodo = $"{NombreMesStatic(p.Mes)} {p.Anio}",
                Mes = p.Mes,
                Anio = p.Anio,
                TotalEmpleados = p.Detalles.Count(d => !d.IsDeleted),
                TotalDevengado = p.TotalDevengado,
                TotalDeducciones = p.TotalDeducciones,
                TotalNeto = p.TotalNeto,
                Estado = p.Estado.ToString(),
                GeneradoPor = p.GeneradoPor ?? "—",
                FechaGeneracion =
                    p.FechaGeneracion.ToString("dd/MM/yyyy"),
                FechaPago = p.FechaPago.HasValue
                    ? p.FechaPago.Value.ToString("dd/MM/yyyy")
                    : null
            })
            .ToListAsync();

        return new DataTablesResponse
        {
            Draw = request.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    // ════════════════════════════════════════════
    // GET BY ID
    // ════════════════════════════════════════════
    public async Task<PlanillaListViewModel?> GetByIdAsync(int id)
    {
        var p = await _db.Planillas
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (p == null) return null;

        return new PlanillaListViewModel
        {
            Id = p.Id,
            Periodo = $"{NombreMes(p.Mes)} {p.Anio}",
            Mes = p.Mes,
            Anio = p.Anio,
            TotalEmpleados = p.Detalles.Count(d => !d.IsDeleted),
            TotalDevengado = p.TotalDevengado,
            TotalDeducciones = p.TotalDeducciones,
            TotalNeto = p.TotalNeto,
            Estado = p.Estado.ToString(),
            GeneradoPor = p.GeneradoPor ?? "—",
            FechaGeneracion =
                p.FechaGeneracion.ToString("dd/MM/yyyy"),
            FechaPago = p.FechaPago?.ToString("dd/MM/yyyy")
        };
    }

    // ════════════════════════════════════════════
    // DETALLES DE PLANILLA
    // ════════════════════════════════════════════
    public async Task<IEnumerable<DetallePlanillaViewModel>>
        GetDetallesAsync(int planillaId)
    {
        return await _db.DetallesPlanilla
            .Include(d => d.Empleado)
                .ThenInclude(e => e.Departamento)
            .Include(d => d.Empleado)
                .ThenInclude(e => e.Puesto)
            .Where(d => d.PlanillaId == planillaId && !d.IsDeleted)
            .OrderBy(d => d.Empleado.PrimerApellido)
            .Select(d => new DetallePlanillaViewModel
            {
                Id = d.Id,
                EmpleadoId = d.EmpleadoId,
                NombreEmpleado = d.Empleado.PrimerNombre + " "
                                  + d.Empleado.PrimerApellido,
                CodigoEmpleado = d.Empleado.Codigo,
                Departamento = d.Empleado.Departamento.Nombre,
                Puesto = d.Empleado.Puesto.Nombre,
                SalarioBase = d.SalarioBase,
                HorasExtraMonto = d.HorasExtraMonto,
                Bonificacion250 = d.Bonificacion250,
                OtrosBonos = d.OtrosBonos,
                Bono14 = d.Bono14,
                Aguinaldo = d.Aguinaldo,
                TotalDevengado = d.TotalDevengado,
                CuotaIGSS = d.CuotaIGSS,
                CuotaIGSSPatronal = d.CuotaIGSSPatronal,
                ISR = d.ISR,
                OtrasDeducciones = d.OtrasDeducciones,
                DescuentoPrestamo = d.DescuentoPrestamo,
                TotalDeducciones = d.TotalDeducciones,
                SalarioNeto = d.SalarioNeto,
                Observacion = d.Observacion
            })
            .ToListAsync();
    }

    // ════════════════════════════════════════════
    // MARCAR PAGADA
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)> MarcarPagadaAsync(
        int id, DateTime fechaPago)
    {
        var planilla = await _db.Planillas
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (planilla == null)
            return (false, "Planilla no encontrada.");

        if (planilla.Estado == EstadoPlanilla.Anulada)
            return (false, "No se puede pagar una planilla anulada.");

        if (planilla.Estado == EstadoPlanilla.Pagada)
            return (false, "Esta planilla ya fue marcada como pagada.");

        planilla.Estado = EstadoPlanilla.Pagada;
        planilla.FechaPago = fechaPago;
        planilla.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Planilla marcada como pagada correctamente.");
    }

    // ════════════════════════════════════════════
    // ANULAR
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)> AnularAsync(int id)
    {
        var planilla = await _db.Planillas
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (planilla == null)
            return (false, "Planilla no encontrada.");

        if (planilla.Estado == EstadoPlanilla.Pagada)
            return (false, "No se puede anular una planilla ya pagada.");

        planilla.Estado = EstadoPlanilla.Anulada;
        planilla.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Planilla anulada correctamente.");
    }

    // ════════════════════════════════════════════
    // ACTUALIZAR DETALLE INDIVIDUAL
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)> ActualizarDetalleAsync(
        int id, DetallePlanillaEditViewModel vm)
    {
        var detalle = await _db.DetallesPlanilla
            .Include(d => d.Planilla)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (detalle == null)
            return (false, "Detalle no encontrado.");

        if (detalle.Planilla.Estado == EstadoPlanilla.Pagada)
            return (false, "No se puede editar una planilla pagada.");

        if (detalle.Planilla.Estado == EstadoPlanilla.Anulada)
            return (false, "No se puede editar una planilla anulada.");

        detalle.OtrosBonos = vm.OtrosBonos;
        detalle.OtrasDeducciones = vm.OtrasDeducciones;
        detalle.Observacion = vm.Observacion;

        detalle.TotalDevengado = detalle.SalarioBase
                               + detalle.HorasExtraMonto
                               + detalle.Bonificacion250
                               + detalle.Bono14
                               + detalle.Aguinaldo
                               + detalle.OtrosBonos;

        detalle.TotalDeducciones = detalle.CuotaIGSS
                                 + detalle.ISR
                                 + detalle.DescuentoPrestamo
                                 + detalle.OtrasDeducciones;

        detalle.SalarioNeto = detalle.TotalDevengado
                            - detalle.TotalDeducciones;
        detalle.UpdatedAt = DateTime.Now;

        var todosDetalles = await _db.DetallesPlanilla
            .Where(d => d.PlanillaId == detalle.PlanillaId
                     && !d.IsDeleted)
            .ToListAsync();

        detalle.Planilla.TotalDevengado = todosDetalles.Sum(d =>
            d.Id == id ? detalle.TotalDevengado : d.TotalDevengado);
        detalle.Planilla.TotalDeducciones = todosDetalles.Sum(d =>
            d.Id == id ? detalle.TotalDeducciones : d.TotalDeducciones);
        detalle.Planilla.TotalNeto = todosDetalles.Sum(d =>
            d.Id == id ? detalle.SalarioNeto : d.SalarioNeto);

        await _db.SaveChangesAsync();
        return (true, "Detalle actualizado correctamente.");
    }

    // ════════════════════════════════════════════
    // RESUMEN ANUAL
    // ════════════════════════════════════════════
    public async Task<ResumenNominaViewModel> GetResumenAnioAsync(int anio)
    {
        var planillas = await _db.Planillas
            .Where(p => p.Anio == anio && !p.IsDeleted
                     && p.Estado != EstadoPlanilla.Anulada)
            .OrderBy(p => p.Mes)
            .ToListAsync();

        return new ResumenNominaViewModel
        {
            Anio = anio,
            TotalPlanillas = planillas.Count,
            TotalPagado = planillas
                .Where(p => p.Estado == EstadoPlanilla.Pagada)
                .Sum(p => p.TotalNeto),
            TotalDevengadoAnio = planillas.Sum(p => p.TotalDevengado),
            TotalDeduccionesAnio = planillas.Sum(p => p.TotalDeducciones),
            PlanillasPorMes = planillas.Select(p =>
                new ResumenMesViewModel
                {
                    Mes = NombreMes(p.Mes),
                    TotalNeto = p.TotalNeto,
                    Estado = p.Estado.ToString()
                }).ToList()
        };
    }

    // ════════════════════════════════════════════
    // HISTORIAL POR EMPLEADO
    // ════════════════════════════════════════════
    public async Task<IEnumerable<PlanillaListViewModel>>
        GetHistorialEmpleadoAsync(int empleadoId)
    {
        return await _db.DetallesPlanilla
            .Include(d => d.Planilla)
            .Where(d => d.EmpleadoId == empleadoId
                     && !d.IsDeleted
                     && d.Planilla.Estado != EstadoPlanilla.Anulada)
            .OrderByDescending(d => d.Planilla.Anio)
            .ThenByDescending(d => d.Planilla.Mes)
            .Select(d => new PlanillaListViewModel
            {
                Id = d.Planilla.Id,
                Periodo =
                    $"{NombreMesStatic(d.Planilla.Mes)} {d.Planilla.Anio}",
                TotalDevengado = d.TotalDevengado,
                TotalDeducciones = d.TotalDeducciones,
                TotalNeto = d.SalarioNeto,
                Estado = d.Planilla.Estado.ToString(),
                FechaPago = d.Planilla.FechaPago.HasValue
                    ? d.Planilla.FechaPago.Value.ToString("dd/MM/yyyy")
                    : null
            })
            .ToListAsync();
    }

    // ════════════════════════════════════════════
    // BOLETA DE PAGO
    // ════════════════════════════════════════════
    public async Task<BoletaPagoViewModel?> GetBoletaPagoAsync(
        int detallePlanillaId)
    {
        var d = await _db.DetallesPlanilla
            .Include(x => x.Planilla)
            .Include(x => x.Empleado)
                .ThenInclude(e => e.Departamento)
            .Include(x => x.Empleado)
                .ThenInclude(e => e.Puesto)
            .FirstOrDefaultAsync(
                x => x.Id == detallePlanillaId && !x.IsDeleted);

        if (d == null) return null;

        return new BoletaPagoViewModel
        {
            EmpleadoId = d.EmpleadoId,
            CodigoEmpleado = d.Empleado.Codigo,
            NombreEmpleado = d.Empleado.NombreCompleto,
            Departamento = d.Empleado.Departamento.Nombre,
            Puesto = d.Empleado.Puesto.Nombre,
            NIT = d.Empleado.NIT ?? "CF",
            NumeroIGSS = d.Empleado.NumeroIGSS ?? "—",
            Periodo =
                $"{NombreMes(d.Planilla.Mes)} {d.Planilla.Anio}",
            FechaPago =
                d.Planilla.FechaPago?.ToString("dd/MM/yyyy") ?? "—",
            SalarioBase = d.SalarioBase,
            HorasExtraMonto = d.HorasExtraMonto,
            Bonificacion250 = d.Bonificacion250,
            OtrosBonos = d.OtrosBonos,
            TotalDevengado = d.TotalDevengado,
            CuotaIGSS = d.CuotaIGSS,
            ISR = d.ISR,
            OtrasDeducciones = d.OtrasDeducciones,
            TotalDeducciones = d.TotalDeducciones,
            SalarioNeto = d.SalarioNeto,
            Observacion = d.Observacion
        };
    }

    // ════════════════════════════════════════════
    // ACTUALIZAR SALARIO CON HISTORIAL
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)> ActualizarSalarioAsync(
        int empleadoId, ActualizarSalarioViewModel vm)
    {
        var emp = await _db.Empleados
            .FirstOrDefaultAsync(e => e.Id == empleadoId
                                   && !e.IsDeleted);

        if (emp == null)
            return (false, "Empleado no encontrado.");

        _db.HistorialesSalario.Add(new HistorialSalario
        {
            EmpleadoId = emp.Id,
            SalarioAnterior = emp.SalarioBase,
            SalarioNuevo = vm.SalarioBase,
            FechaCambio = DateTime.Today,
            Motivo = vm.Motivo ?? "Actualización de salario",
            CambiadoPor = null
        });

        emp.SalarioBase = vm.SalarioBase;
        emp.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Salario actualizado correctamente.");
    }

    // ════════════════════════════════════════════
    // PRÉSTAMOS
    // ════════════════════════════════════════════
    public async Task<IEnumerable<PrestamoListViewModel>>
        GetPrestamosAsync()
    {
        return await _db.PrestamosEmpleado
            .Include(p => p.Empleado)
                .ThenInclude(e => e.Departamento)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrestamoListViewModel
            {
                Id = p.Id,
                NombreEmpleado = p.Empleado.PrimerNombre
                               + " " + p.Empleado.PrimerApellido,
                Departamento = p.Empleado.Departamento.Nombre,
                MontoTotal = p.MontoTotal,
                CuotaMensual = p.CuotaMensual,
                NumeroCuotas = p.NumeroCuotas,
                CuotasPagadas = p.CuotasPagadas,
                SaldoPendiente = p.SaldoPendiente,
                Estado = p.Estado.ToString(),
                FechaInicio = p.FechaInicio.ToString("dd/MM/yyyy"),
                Motivo = p.Motivo
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> CrearPrestamoAsync(
        PrestamoViewModel vm)
    {
        var emp = await _db.Empleados
            .FirstOrDefaultAsync(e => e.Id == vm.EmpleadoId
                                   && !e.IsDeleted);
        if (emp == null)
            return (false, "Empleado no encontrado.");

        var tieneActivo = await _db.PrestamosEmpleado
            .AnyAsync(p => p.EmpleadoId == vm.EmpleadoId
                        && p.Estado == EstadoPrestamo.Activo
                        && !p.IsDeleted);

        if (tieneActivo)
            return (false, "El empleado ya tiene un préstamo activo.");

        DateTime.TryParse(vm.FechaInicio, out var fechaInicio);

        _db.PrestamosEmpleado.Add(new PrestamoEmpleado
        {
            EmpleadoId = vm.EmpleadoId,
            MontoTotal = vm.MontoTotal,
            CuotaMensual = vm.CuotaMensual,
            NumeroCuotas = vm.NumeroCuotas,
            CuotasPagadas = 0,
            SaldoPendiente = vm.MontoTotal,
            FechaInicio = fechaInicio == default
                ? DateTime.Today : fechaInicio,
            FechaFinEstimada = fechaInicio == default
                ? DateTime.Today.AddMonths(vm.NumeroCuotas)
                : fechaInicio.AddMonths(vm.NumeroCuotas),
            Estado = EstadoPrestamo.Activo,
            Motivo = vm.Motivo,
            AutorizadoPor = "Sistema"
        });

        await _db.SaveChangesAsync();
        return (true, "Préstamo registrado correctamente.");
    }

    public async Task<(bool success, string message)> CancelarPrestamoAsync(
        int id)
    {
        var prestamo = await _db.PrestamosEmpleado
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (prestamo == null)
            return (false, "Préstamo no encontrado.");

        prestamo.Estado = EstadoPrestamo.Cancelado;
        prestamo.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Préstamo cancelado.");
    }

    public async Task<(bool success, string message)> AbonarCuotaAsync(
        int id, decimal monto)
    {
        var prestamo = await _db.PrestamosEmpleado
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (prestamo == null)
            return (false, "Préstamo no encontrado.");

        if (prestamo.Estado != EstadoPrestamo.Activo)
            return (false, "Solo se pueden abonar préstamos activos.");

        if (monto <= 0)
            return (false, "El monto del abono debe ser mayor a Q0.");

        if (monto > prestamo.SaldoPendiente)
            return (false,
                $"El abono no puede superar el saldo pendiente " +
                $"de Q{prestamo.SaldoPendiente:N2}.");

        prestamo.SaldoPendiente -= monto;
        prestamo.UpdatedAt = DateTime.Now;

        if (Math.Abs(monto - prestamo.CuotaMensual) < 0.10m)
            prestamo.CuotasPagadas += 1;

        if (prestamo.SaldoPendiente <= 0)
        {
            prestamo.SaldoPendiente = 0;
            prestamo.CuotasPagadas = prestamo.NumeroCuotas;
            prestamo.Estado = EstadoPrestamo.Completado;
            await _db.SaveChangesAsync();
            return (true,
                "Préstamo liquidado completamente. ¡Felicitaciones!");
        }

        await _db.SaveChangesAsync();
        return (true,
            $"Abono de Q{monto:N2} registrado. " +
            $"Saldo pendiente: Q{prestamo.SaldoPendiente:N2}.");
    }

    public async Task<(bool success, string message)> PagarDeudaCompletaAsync(
        int id)
    {
        var prestamo = await _db.PrestamosEmpleado
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (prestamo == null)
            return (false, "Préstamo no encontrado.");

        if (prestamo.Estado != EstadoPrestamo.Activo)
            return (false, "Solo se pueden liquidar préstamos activos.");

        prestamo.SaldoPendiente = 0;
        prestamo.CuotasPagadas = prestamo.NumeroCuotas;
        prestamo.Estado = EstadoPrestamo.Completado;
        prestamo.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Deuda liquidada completamente.");
    }

    public async Task<(bool success, string message)> EliminarPrestamoAsync(
        int id)
    {
        var prestamo = await _db.PrestamosEmpleado
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (prestamo == null)
            return (false, "Préstamo no encontrado.");

        if (prestamo.Estado != EstadoPrestamo.Cancelado
         && prestamo.Estado != EstadoPrestamo.Completado)
            return (false,
                "Solo se pueden eliminar préstamos " +
                "cancelados o liquidados.");

        prestamo.IsDeleted = true;
        prestamo.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Préstamo eliminado correctamente.");
    }

    // ════════════════════════════════════════════
    // CONCEPTOS
    // ════════════════════════════════════════════
    public async Task<IEnumerable<ConceptoListViewModel>>
        GetConceptosAsync()
    {
        return await _db.ConceptosNomina
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Tipo)
            .ThenBy(c => c.Nombre)
            .Select(c => new ConceptoListViewModel
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Tipo = c.Tipo.ToString(),
                Aplicacion = c.Aplicacion.ToString(),
                Valor = c.Valor,
                EsObligatorio = c.EsObligatorio,
                EsSistema = c.EsSistema,
                Activo = c.Activo
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> CrearConceptoAsync(
        ConceptoNominaViewModel vm)
    {
        var existe = await _db.ConceptosNomina
            .AnyAsync(c => c.Codigo == vm.Codigo && !c.IsDeleted);

        if (existe)
            return (false,
                $"Ya existe un concepto con el código {vm.Codigo}.");

        Enum.TryParse<TipoConcepto>(vm.Tipo, out var tipo);
        Enum.TryParse<AplicacionConcepto>(vm.Aplicacion, out var aplic);

        _db.ConceptosNomina.Add(new ConceptoNomina
        {
            Codigo = vm.Codigo.ToUpper().Trim(),
            Nombre = vm.Nombre,
            Descripcion = vm.Descripcion,
            Tipo = tipo,
            Aplicacion = aplic,
            Valor = vm.Valor,
            EsObligatorio = false,
            EsSistema = false,
            Activo = true
        });

        await _db.SaveChangesAsync();
        return (true, "Concepto creado correctamente.");
    }

    public async Task<(bool success, string message)> EditarConceptoAsync(
        int id, ConceptoNominaViewModel vm)
    {
        var concepto = await _db.ConceptosNomina
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (concepto == null)
            return (false, "Concepto no encontrado.");

        if (concepto.EsSistema)
            return (false,
                "Los conceptos del sistema no se pueden editar.");

        Enum.TryParse<TipoConcepto>(vm.Tipo, out var tipo);
        Enum.TryParse<AplicacionConcepto>(vm.Aplicacion, out var aplic);

        concepto.Nombre = vm.Nombre;
        concepto.Descripcion = vm.Descripcion;
        concepto.Tipo = tipo;
        concepto.Aplicacion = aplic;
        concepto.Valor = vm.Valor;
        concepto.Activo = vm.Activo;
        concepto.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Concepto actualizado correctamente.");
    }

    public async Task<(bool success, string message)> EliminarConceptoAsync(
        int id)
    {
        var concepto = await _db.ConceptosNomina
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (concepto == null)
            return (false, "Concepto no encontrado.");

        if (concepto.EsSistema)
            return (false,
                "Los conceptos del sistema no se pueden eliminar.");

        concepto.IsDeleted = true;
        concepto.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Concepto eliminado correctamente.");
    }

    public async Task<(bool success, string message)> EliminarPlanillaAsync(int id)
    {
        var planilla = await _db.Planillas
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (planilla == null)
            return (false, "Planilla no encontrada.");

        if (planilla.Estado != EstadoPlanilla.Anulada)
            return (false, "Solo se pueden eliminar planillas anuladas.");

        planilla.IsDeleted = true;
        planilla.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return (true, "Planilla eliminada correctamente.");
    }

    // ════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ════════════════════════════════════════════
    private static string NombreMes(int mes) => NombreMesStatic(mes);

    private static string NombreMesStatic(int mes) => mes switch
    {
        1 => "Enero",
        2 => "Febrero",
        3 => "Marzo",
        4 => "Abril",
        5 => "Mayo",
        6 => "Junio",
        7 => "Julio",
        8 => "Agosto",
        9 => "Septiembre",
        10 => "Octubre",
        11 => "Noviembre",
        12 => "Diciembre",
        _ => "—"
    };
}