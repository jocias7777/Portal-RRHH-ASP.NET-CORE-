using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class ReportesService : IReportesService
{
    private readonly ApplicationDbContext _context;

    public ReportesService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ════════════════════════════════════════════
    // NÓMINA Y COSTOS LABORALES
    // ════════════════════════════════════════════

    public async Task<List<ReportePlanillaMensualViewModel>> GetPlanillaMensualAsync(int mes, int anio, int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Puesto)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();

        // Obtener planilla del periodo si existe
        var planilla = await _context.Planillas
            .Include(p => p.Detalles)
            .ThenInclude(d => d.Empleado)
            .FirstOrDefaultAsync(p => p.Mes == mes && p.Anio == anio);

        var resultado = new List<ReportePlanillaMensualViewModel>();

        foreach (var emp in empleados)
        {
            var detalle = planilla?.Detalles.FirstOrDefault(d => d.EmpleadoId == emp.Id);

            decimal igssLaboral = detalle?.CuotaIGSS ?? (emp.SalarioBase * 0.0483m);
            decimal igssPatronal = detalle?.CuotaIGSSPatronal ?? (emp.SalarioBase * 0.1267m);
            decimal totalDevengado = detalle?.TotalDevengado ?? emp.SalarioBase + 250; // + bonificación incentivo
            decimal totalDeducciones = detalle?.TotalDeducciones ?? igssLaboral;
            decimal salarioNeto = detalle?.SalarioNeto ?? (totalDevengado - totalDeducciones);

            resultado.Add(new ReportePlanillaMensualViewModel
            {
                EmpleadoId = emp.Id,
                Codigo = emp.Codigo,
                NombreCompleto = emp.NombreCompleto,
                Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                Puesto = emp.Puesto?.Nombre ?? "Sin asignar",
                SalarioBase = emp.SalarioBase,
                HorasExtra = detalle?.HorasExtraMonto ?? 0,
                Bonificacion250 = detalle?.Bonificacion250 ?? 250,
                TotalDevengado = totalDevengado,
                IGSSLaboral = igssLaboral,
                ISR = detalle?.ISR ?? 0,
                TotalDeducciones = totalDeducciones,
                SalarioNeto = salarioNeto,
                IGSSPatronal = igssPatronal,
                CostoTotal = salarioNeto + igssPatronal
            });
        }

        return resultado;
    }

    public async Task<List<ResumenBono14AguinaldoViewModel>> GetBono14AguinaldoProyectadoAsync(int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();
        var hoy = DateTime.Today;

        return empleados.Select(emp =>
        {
            var mesesTrabajados = Math.Min(12, (int)((hoy - emp.FechaIngreso).TotalDays / 30));
            var proporcion = mesesTrabajados / 12m;

            var bono14 = emp.SalarioBase * proporcion;
            var aguinaldo = emp.SalarioBase * proporcion;

            return new ResumenBono14AguinaldoViewModel
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                FechaIngreso = emp.FechaIngreso,
                SalarioBase = emp.SalarioBase,
                Bono14Proyectado = bono14,
                AguinaldoProyectado = aguinaldo,
                TotalPrestaciones = bono14 + aguinaldo,
                MesesTrabajados = mesesTrabajados
            };
        }).ToList();
    }

    public async Task<List<CostoPorDepartamentoViewModel>> GetCostoPorDepartamentoAsync(int anio)
    {
        var departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .ToListAsync();

        var resultado = new List<CostoPorDepartamentoViewModel>();

        foreach (var depto in departamentos)
        {
            var empleados = await _context.Empleados
                .Where(e => !e.IsDeleted && e.DepartamentoId == depto.Id && e.Estado == EstadoEmpleado.Activo)
                .ToListAsync();

            var totalSalarios = empleados.Sum(e => e.SalarioBase);
            var totalIGSSPatronal = totalSalarios * 0.1267m;
            var totalBono14 = totalSalarios; // 1 salario anual
            var totalAguinaldo = totalSalarios; // 1 salario anual
            var totalPrestaciones = totalIGSSPatronal + totalBono14 + totalAguinaldo;

            resultado.Add(new CostoPorDepartamentoViewModel
            {
                DepartamentoId = depto.Id,
                Departamento = depto.Nombre,
                CantidadEmpleados = empleados.Count,
                TotalSalarios = totalSalarios,
                TotalIGSSPatronal = totalIGSSPatronal,
                TotalBono14 = totalBono14,
                TotalAguinaldo = totalAguinaldo,
                TotalPrestaciones = totalPrestaciones,
                CostoTotalDepartamento = totalSalarios + totalPrestaciones
            });
        }

        return resultado.OrderBy(d => d.Departamento).ToList();
    }

    // ════════════════════════════════════════════
    // CUMPLIMIENTO LEGAL
    // ════════════════════════════════════════════

    public async Task<List<ContratoPorVencerViewModel>> GetContratosPorVencerAsync(int dias = 90)
    {
        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Puesto)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var hoy = DateTime.Today;
        var resultado = new List<ContratoPorVencerViewModel>();

        foreach (var emp in empleados)
        {
            // Solo contratos temporales o por obra tienen vencimiento
            if (emp.TipoContrato == TipoContrato.Indefinido)
                continue;

            // Calcular fecha de vencimiento (1 año desde ingreso para temporal)
            DateTime? fechaVencimiento = emp.FechaIngreso.AddYears(1);
            int diasParaVencer = (fechaVencimiento.Value - hoy).Days;

            if (diasParaVencer >= 0 && diasParaVencer <= dias)
            {
                string alerta = diasParaVencer <= 30 ? "30 días" : diasParaVencer <= 60 ? "60 días" : "90 días";

                resultado.Add(new ContratoPorVencerViewModel
                {
                    EmpleadoId = emp.Id,
                    NombreCompleto = emp.NombreCompleto,
                    Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                    Puesto = emp.Puesto?.Nombre ?? "Sin asignar",
                    TipoContrato = emp.TipoContrato,
                    FechaIngreso = emp.FechaIngreso,
                    FechaVencimiento = fechaVencimiento,
                    DiasParaVencer = diasParaVencer,
                    Alerta = alerta
                });
            }
        }

        return resultado.OrderBy(d => d.DiasParaVencer).ToList();
    }

    public async Task<List<EmpleadoSinDocumentosViewModel>> GetEmpleadosSinDocumentosAsync()
    {
        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Documentos)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var resultado = new List<EmpleadoSinDocumentosViewModel>();

        foreach (var emp in empleados)
        {
            var faltantes = new List<string>();

            if (string.IsNullOrWhiteSpace(emp.CUI))
                faltantes.Add("CUI/DPI");

            if (string.IsNullOrWhiteSpace(emp.NumeroIGSS))
                faltantes.Add("IGSS");

            if (string.IsNullOrWhiteSpace(emp.NIT))
                faltantes.Add("NIT");

            // Verificar si tiene documento de antecedentes
            var tieneAntecedentes = emp.Documentos.Any(d => d.Tipo == TipoDocumento.Antecedentes && d.Estado == EstadoDocumento.Activo);
            if (!tieneAntecedentes)
                faltantes.Add("Antecedentes");

            if (faltantes.Count > 0)
            {
                resultado.Add(new EmpleadoSinDocumentosViewModel
                {
                    EmpleadoId = emp.Id,
                    NombreCompleto = emp.NombreCompleto,
                    Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                    Email = emp.Email,
                    TieneCUI = !string.IsNullOrWhiteSpace(emp.CUI),
                    TieneIGSS = !string.IsNullOrWhiteSpace(emp.NumeroIGSS),
                    TieneNIT = !string.IsNullOrWhiteSpace(emp.NIT),
                    TieneAntecedentes = tieneAntecedentes,
                    DocumentosFaltantes = faltantes
                });
            }
        }

        return resultado.OrderBy(e => e.NombreCompleto).ToList();
    }

    public async Task<ReporteNacionalidadViewModel> GetNacionalidadReporteAsync()
    {
        var empleados = await _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var total = empleados.Count;

        // Asumimos que los extranjeros tienen CUI que no empieza con los dígitos de Guatemala
        // En producción esto debería tener un campo explicito de nacionalidad
        var guatemaltecos = empleados.Count(e => e.CUI.StartsWith("1")); // CUI Guatemala inicia con 1
        var extranjeros = total - guatemaltecos;

        var porcGuatemaltecos = total > 0 ? (guatemaltecos * 100m / total) : 0;
        var porcExtranjeros = total > 0 ? (extranjeros * 100m / total) : 0;

        // Art. 14 Código de Trabajo: máximo 10% extranjeros
        var cumpleArt14 = porcExtranjeros <= 10;

        return new ReporteNacionalidadViewModel
        {
            TotalEmpleados = total,
            EmpleadosGuatemaltecos = guatemaltecos,
            EmpleadosExtranjeros = extranjeros,
            PorcentajeGuatemaltecos = porcGuatemaltecos,
            PorcentajeExtranjeros = porcExtranjeros,
            CumpleArticulo14 = cumpleArt14,
            Observacion = cumpleArt14
                ? "Cumple con Art. 14 del Código de Trabajo"
                : $"NO CUMPLE Art. 14 CT - Excede 10% extranjeros ({porcExtranjeros:F2}%)"
        };
    }

    public async Task<List<VacacionesNoTomadasViewModel>> GetVacacionesNoTomadasAsync()
    {
        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var hoy = DateTime.Today;
        var anioActual = hoy.Year;

        var resultado = new List<VacacionesNoTomadasViewModel>();

        foreach (var emp in empleados)
        {
            var antiguedadAnios = (int)((hoy - emp.FechaIngreso).TotalDays / 365);
            var tieneDerecho = antiguedadAnios >= 1;

            if (!tieneDerecho)
                continue;

            // Vacaciones anuales = 15 días hábiles por ley GT
            int diasDisponibles = 15;

            // Obtener vacaciones tomadas en el año actual
            var vacacionesTomadas = await _context.Vacaciones
                .Where(v => v.EmpleadoId == emp.Id &&
                           v.FechaInicio.Year == anioActual &&
                           v.Estado == EstadoVacacion.Aprobado)
                .SumAsync(v => v.DiasSolicitados);

            // Última vacaciones
            var ultimaVacacion = await _context.Vacaciones
                .Where(v => v.EmpleadoId == emp.Id)
                .OrderByDescending(v => v.FechaInicio)
                .FirstOrDefaultAsync();

            int diasNoTomados = diasDisponibles - vacacionesTomadas;

            if (diasNoTomados > 0)
            {
                resultado.Add(new VacacionesNoTomadasViewModel
                {
                    EmpleadoId = emp.Id,
                    NombreCompleto = emp.NombreCompleto,
                    Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                    FechaIngreso = emp.FechaIngreso,
                    DiasDisponibles = diasDisponibles,
                    DiasTomadosAnioActual = vacacionesTomadas,
                    UltimaVacacionFecha = ultimaVacacion?.FechaInicio,
                    TieneDerecho = tieneDerecho
                });
            }
        }

        return resultado.OrderBy(e => e.NombreCompleto).ToList();
    }

    // ════════════════════════════════════════════
    // ASISTENCIA Y TIEMPO
    // ════════════════════════════════════════════

    public async Task<List<ReporteInasistenciasTardanzasViewModel>> GetInasistenciasTardanzasAsync(DateTime desde, DateTime hasta, int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();
        var totalDiasPeriodo = (hasta - desde).Days + 1;

        var resultado = new List<ReporteInasistenciasTardanzasViewModel>();

        foreach (var emp in empleados)
        {
            var asistencias = await _context.Asistencias
                .Where(a => a.EmpleadoId == emp.Id &&
                           a.Fecha >= desde && a.Fecha <= hasta)
                .ToListAsync();

            var presentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente);
            var ausentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);
            var tardanzas = asistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
            var justificados = asistencias.Count(a => a.Estado == EstadoAsistencia.PermisoJustificado);
            var horasExtra = asistencias.Sum(a => a.HorasExtra);

            var porcentaje = totalDiasPeriodo > 0
                ? ((presentes + (tardanzas * 0.5m)) * 100 / totalDiasPeriodo)
                : 0;

            var estado = porcentaje >= 90 ? "Normal" : "Riesgo";

            resultado.Add(new ReporteInasistenciasTardanzasViewModel
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                TotalDiasPeriodo = totalDiasPeriodo,
                DiasPresentes = presentes,
                DiasAusentes = ausentes,
                DiasTardanza = tardanzas,
                DiasJustificados = justificados,
                PorcentajeAsistencia = porcentaje,
                TotalHorasExtra = horasExtra,
                Estado = estado
            });
        }

        return resultado.OrderBy(e => e.PorcentajeAsistencia).ToList();
    }

    public async Task<List<HorasExtraPorEmpleadoViewModel>> GetHorasExtraPorEmpleadoAsync(DateTime desde, DateTime hasta, int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();
        var resultado = new List<HorasExtraPorEmpleadoViewModel>();

        foreach (var emp in empleados)
        {
            var asistencias = await _context.Asistencias
                .Where(a => a.EmpleadoId == emp.Id &&
                           a.Fecha >= desde && a.Fecha <= hasta &&
                           a.HorasExtra > 0)
                .ToListAsync();

            var totalHoras = asistencias.Sum(a => a.HorasExtra);
            var monto = totalHoras * (emp.SalarioBase / 240) * 1.5m; // Valor hora extra 150%

            if (totalHoras > 0)
            {
                resultado.Add(new HorasExtraPorEmpleadoViewModel
                {
                    EmpleadoId = emp.Id,
                    NombreCompleto = emp.NombreCompleto,
                    Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                    TotalHorasExtraMes = totalHoras,
                    MontoHorasExtra = monto,
                    CantidadDiasConHoraExtra = asistencias.Count
                });
            }
        }

        return resultado.OrderByDescending(e => e.TotalHorasExtraMes).ToList();
    }

    public async Task<List<EmpleadoConMasDeTresFaltasViewModel>> GetEmpleadosConMasDeTresFaltasAsync(int mes, int anio)
    {
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddDays(-1);

        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var resultado = new List<EmpleadoConMasDeTresFaltasViewModel>();

        foreach (var emp in empleados)
        {
            var faltas = await _context.Asistencias
                .Where(a => a.EmpleadoId == emp.Id &&
                           a.Fecha >= desde && a.Fecha <= hasta &&
                           (a.Estado == EstadoAsistencia.Ausente || a.Estado == EstadoAsistencia.Tardanza))
                .OrderBy(a => a.Fecha)
                .ToListAsync();

            if (faltas.Count >= 3)
            {
                // Verificar reincidencia (más de 3 faltas en meses anteriores)
                var faltasPrevias = await _context.Asistencias
                    .Where(a => a.EmpleadoId == emp.Id &&
                               a.Fecha < desde &&
                               (a.Estado == EstadoAsistencia.Ausente || a.Estado == EstadoAsistencia.Tardanza))
                    .CountAsync();

                var esReincidente = faltasPrevias >= 3;

                resultado.Add(new EmpleadoConMasDeTresFaltasViewModel
                {
                    EmpleadoId = emp.Id,
                    NombreCompleto = emp.NombreCompleto,
                    Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                    Email = emp.Email,
                    TotalFaltasMes = faltas.Count,
                    FechasFalta = faltas.Select(f => f.Fecha).ToList(),
                    EsReincidente = esReincidente
                });
            }
        }

        return resultado.OrderByDescending(e => e.TotalFaltasMes).ToList();
    }

    // ════════════════════════════════════════════
    // ROTACIÓN DE PERSONAL
    // ════════════════════════════════════════════

    public async Task<List<AltasBajasViewModel>> GetAltasBajasPorPeriodoAsync(DateTime desde, DateTime hasta)
    {
        var resultado = new List<AltasBajasViewModel>();
        var actual = desde;

        while (actual <= hasta)
        {
            var mes = actual.Month;
            var anio = actual.Year;
            var inicioMes = new DateTime(anio, mes, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            // Altas: empleados creados en el mes
            var altas = await _context.Empleados
                .Where(e => e.CreatedAt >= inicioMes && e.CreatedAt <= finMes && !e.IsDeleted)
                .CountAsync();

            // Bajas: empleados con fecha de salida en el mes
            var bajas = await _context.Empleados
                .Where(e => e.FechaSalida.HasValue &&
                           e.FechaSalida.Value >= inicioMes && e.FechaSalida.Value <= finMes)
                .CountAsync();

            // Promedio de empleados
            var promedio = await _context.Empleados
                .Where(e => e.FechaIngreso <= finMes &&
                           (e.FechaSalida == null || e.FechaSalida >= inicioMes))
                .CountAsync();

            var tasaRotacion = promedio > 0 ? (bajas * 100m / promedio) : 0;

            resultado.Add(new AltasBajasViewModel
            {
                Periodo = inicioMes,
                Altas = altas,
                Bajas = bajas,
                Neto = altas - bajas,
                TasaRotacion = tasaRotacion
            });

            actual = actual.AddMonths(1);
        }

        return resultado;
    }

    public async Task<List<MotivoSalidaViewModel>> GetMotivosSalidaAsync(DateTime desde, DateTime hasta)
    {
        // En producción debería haber un campo MotivoSalida en Empleado
        // Por ahora usamos un enfoque simplificado
        var bajas = await _context.Empleados
            .Where(e => e.FechaSalida.HasValue &&
                       e.FechaSalida.Value >= desde &&
                       e.FechaSalida.Value <= hasta)
            .ToListAsync();

        // Agrupar por tipo de salida (simulado)
        var resultado = new List<MotivoSalidaViewModel>();

        // Renuncia voluntaria
        var renuncias = bajas.Count(e => e.Estado == EstadoEmpleado.Inactivo);
        // Despido
        var despidos = bajas.Count(e => e.Estado == EstadoEmpleado.Baja);

        if (renuncias > 0)
        {
            resultado.Add(new MotivoSalidaViewModel
            {
                Motivo = "Renuncia Voluntaria",
                Cantidad = renuncias,
                Porcentaje = bajas.Count > 0 ? renuncias * 100m / bajas.Count : 0,
                TotalIndemnizaciones = 0 // Renuncia no lleva indemnización
            });
        }

        if (despidos > 0)
        {
            // Calcular indemnizaciones estimadas
            var totalIndem = await CalcularIndemnizacionesAsync(bajas.Where(e => e.Estado == EstadoEmpleado.Baja));

            resultado.Add(new MotivoSalidaViewModel
            {
                Motivo = "Despido",
                Cantidad = despidos,
                Porcentaje = bajas.Count > 0 ? despidos * 100m / bajas.Count : 0,
                TotalIndemnizaciones = totalIndem
            });
        }

        return resultado.OrderByDescending(m => m.Cantidad).ToList();
    }

    private async Task<decimal> CalcularIndemnizacionesAsync(IEnumerable<Empleado> empleados)
    {
        decimal total = 0;
        foreach (var emp in empleados)
        {
            var aniosServicio = (int)((DateTime.Today - emp.FechaIngreso).TotalDays / 365);
            total += emp.SalarioBase * aniosServicio;
        }
        return await Task.FromResult(total);
    }

    public async Task<List<TiempoPermanenciaViewModel>> GetTiempoPermanenciaPorDepartamentoAsync()
    {
        var departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .ToListAsync();

        var resultado = new List<TiempoPermanenciaViewModel>();
        var hoy = DateTime.Today;

        foreach (var depto in departamentos)
        {
            var empleados = await _context.Empleados
                .Where(e => !e.IsDeleted && e.DepartamentoId == depto.Id && e.Estado == EstadoEmpleado.Activo)
                .ToListAsync();

            if (empleados.Count == 0)
                continue;

            var tiempoTotalMeses = empleados.Sum(e => (int)((hoy - e.FechaIngreso).TotalDays / 30));
            var tiempoPromedio = tiempoTotalMeses / empleados.Count;

            var rango = new EmpleadoAntiguedadRango
            {
                Menos1Anio = empleados.Count(e => (hoy - e.FechaIngreso).TotalDays < 365),
                De1a3Anios = empleados.Count(e => (hoy - e.FechaIngreso).TotalDays >= 365 && (hoy - e.FechaIngreso).TotalDays < 365 * 3),
                De3a5Anios = empleados.Count(e => (hoy - e.FechaIngreso).TotalDays >= 365 * 3 && (hoy - e.FechaIngreso).TotalDays < 365 * 5),
                MasDe5Anios = empleados.Count(e => (hoy - e.FechaIngreso).TotalDays >= 365 * 5)
            };

            resultado.Add(new TiempoPermanenciaViewModel
            {
                Departamento = depto.Nombre,
                CantidadEmpleados = empleados.Count,
                TiempoPromedioAnios = tiempoPromedio / 12,
                TiempoPromedioMeses = tiempoPromedio % 12,
                RangoAntiguedad = rango
            });
        }

        return resultado.OrderBy(d => d.Departamento).ToList();
    }

    // ════════════════════════════════════════════
    // PRESTACIONES E INDEMNIZACIONES
    // ════════════════════════════════════════════

    public async Task<List<ProyeccionIndemnizacionViewModel>> GetProyeccionIndemnizacionAsync(int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();
        var hoy = DateTime.Today;

        var resultado = new List<ProyeccionIndemnizacionViewModel>();

        foreach (var emp in empleados)
        {
            var aniosServicio = (int)((hoy - emp.FechaIngreso).TotalDays / 365);
            var mesesAdicionales = (int)((hoy - emp.FechaIngreso.AddYears(aniosServicio)).TotalDays / 30);

            // Indemnización: 1 salario por año (mínimo legal GT)
            var indemAnios = emp.SalarioBase * aniosServicio;
            var indemProporcional = (emp.SalarioBase / 12) * mesesAdicionales;

            // Vacaciones no gozadas (15 días por año)
            var vacacionesNoGozadas = (emp.SalarioBase / 30) * 15;

            // Aguinaldo y Bono 14 proporcionales
            var mesActual = hoy.Month;
            var aguinaldoProp = (emp.SalarioBase / 12) * mesActual;
            var bono14Prop = (emp.SalarioBase / 12) * mesActual;

            var total = indemAnios + indemProporcional + vacacionesNoGozadas + aguinaldoProp + bono14Prop;

            resultado.Add(new ProyeccionIndemnizacionViewModel
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                FechaIngreso = emp.FechaIngreso,
                SalarioBase = emp.SalarioBase,
                SalarioPromedio6Meses = emp.SalarioBase, // Simplificado
                AniosServicio = aniosServicio,
                MesesAdicionales = mesesAdicionales,
                IndemnizacionPorAnios = indemAnios,
                IndemnizacionProporcional = indemProporcional,
                VacacionesNoGozadas = vacacionesNoGozadas,
                AguinaldoProporcional = aguinaldoProp,
                Bono14Proporcional = bono14Prop,
                TotalIndemnificacion = total
            });
        }

        return resultado.OrderBy(e => e.NombreCompleto).ToList();
    }

    public async Task<List<VacacionesAcumuladasViewModel>> GetVacacionesAcumuladasValorizadasAsync(int? departamentoId = null)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo);

        if (departamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == departamentoId);

        var empleados = await query.ToListAsync();
        var hoy = DateTime.Today;
        var anioActual = hoy.Year;

        var resultado = new List<VacacionesAcumuladasViewModel>();

        foreach (var emp in empleados)
        {
            var antiguedadAnios = (int)((hoy - emp.FechaIngreso).TotalDays / 365);
            if (antiguedadAnios < 1)
                continue;

            // 15 días por año de vacaciones
            int diasAcumulados = antiguedadAnios * 15;

            // Restar vacaciones tomadas
            var tomadas = await _context.Vacaciones
                .Where(v => v.EmpleadoId == emp.Id && v.Estado == EstadoVacacion.Aprobado)
                .SumAsync(v => v.DiasSolicitados);

            diasAcumulados -= tomadas;

            if (diasAcumulados <= 0)
                continue;

            var valorDiario = emp.SalarioBase / 30;
            var valorQuetzales = diasAcumulados * valorDiario;

            resultado.Add(new VacacionesAcumuladasViewModel
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                Departamento = emp.Departamento?.Nombre ?? "Sin asignar",
                FechaIngreso = emp.FechaIngreso,
                DiasAcumulados = diasAcumulados,
                DiasTomadosAnio = tomadas,
                ValorDiario = valorDiario,
                ValorQuetzales = valorQuetzales
            });
        }

        return resultado.OrderByDescending(e => e.ValorQuetzales).ToList();
    }

    public async Task<List<FiniquitoEmitidoViewModel>> GetFiniquitosEmitidosAsync(DateTime desde, DateTime hasta)
    {
        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted &&
                       e.FechaSalida.HasValue &&
                       e.FechaSalida.Value >= desde &&
                       e.FechaSalida.Value <= hasta)
            .ToListAsync();

        var resultado = new List<FiniquitoEmitidoViewModel>();

        foreach (var emp in empleados)
        {
            var diasTrabajadosMes = (int)((emp.FechaSalida.Value.Day - 1) * (emp.SalarioBase / 30));
            var vacacionesNoGozadas = 15 * (emp.SalarioBase / 30); // Simplificado
            var aguinaldoProp = (emp.SalarioBase / 12) * emp.FechaSalida.Value.Month;
            var bono14Prop = (emp.SalarioBase / 12) * emp.FechaSalida.Value.Month;

            var totalDevengado = diasTrabajadosMes + vacacionesNoGozadas + aguinaldoProp + bono14Prop;
            var igss = totalDevengado * 0.0483m;
            var totalNeto = totalDevengado - igss;

            resultado.Add(new FiniquitoEmitidoViewModel
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                FechaSalida = emp.FechaSalida.Value,
                MotivoSalida = emp.Estado == EstadoEmpleado.Baja ? "Despido" : "Renuncia",
                SalarioDevengado = diasTrabajadosMes,
                VacacionesNoGozadas = vacacionesNoGozadas,
                AguinaldoProporcional = aguinaldoProp,
                Bono14Proporcional = bono14Prop,
                TotalDeducciones = igss,
                TotalNetoPagar = totalNeto,
                FechaEmision = emp.FechaSalida.Value
            });
        }

        return resultado.OrderByDescending(f => f.FechaEmision).ToList();
    }

    // ════════════════════════════════════════════
    // EXPEDIENTES
    // ════════════════════════════════════════════

    public async Task<List<CompletitudExpedienteViewModel>> GetCompletitudExpedientesAsync()
    {
        var departamentos = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .ToListAsync();

        var resultado = new List<CompletitudExpedienteViewModel>();

        // Documentos requeridos por expediente
        var documentosRequeridos = new[]
        {
            TipoDocumento.Contrato,
            TipoDocumento.Certificado,
            TipoDocumento.Antecedentes,
            TipoDocumento.Credencial
        };

        foreach (var depto in departamentos)
        {
            var empleados = await _context.Empleados
                .Include(e => e.Documentos)
                .Where(e => !e.IsDeleted && e.DepartamentoId == depto.Id)
                .ToListAsync();

            var empleadosSinExpedienteCompleto = new List<string>();
            int expedientesCompletos = 0;

            foreach (var emp in empleados)
            {
                var docsTipo = emp.Documentos
                    .Where(d => d.Estado == EstadoDocumento.Activo)
                    .Select(d => d.Tipo)
                    .ToHashSet();

                var tieneTodos = documentosRequeridos.All(t => docsTipo.Contains(t));

                if (tieneTodos)
                {
                    expedientesCompletos++;
                }
                else
                {
                    empleadosSinExpedienteCompleto.Add(emp.NombreCompleto);
                }
            }

            var total = empleados.Count;
            var porcentaje = total > 0 ? (expedientesCompletos * 100m / total) : 0;

            resultado.Add(new CompletitudExpedienteViewModel
            {
                DepartamentoId = depto.Id,
                Departamento = depto.Nombre,
                TotalEmpleados = total,
                ExpedientesCompletos = expedientesCompletos,
                ExpedientesIncompletos = total - expedientesCompletos,
                PorcentajeCompletitud = porcentaje,
                EmpleadosSinExpedienteCompleto = empleadosSinExpedienteCompleto
            });
        }

        return resultado.OrderBy(d => d.PorcentajeCompletitud).ToList();
    }

    public async Task<List<DocumentoVencidoViewModel>> GetDocumentosVencidosOPorVencerAsync(int dias = 30)
    {
        var documentos = await _context.Documentos
            .Include(d => d.Empleado)
            .ThenInclude(e => e.Departamento)
            .Where(d => !d.IsDeleted && d.FechaExpiracion.HasValue)
            .ToListAsync();

        var hoy = DateTime.Today;
        var resultado = new List<DocumentoVencidoViewModel>();

        foreach (var doc in documentos)
        {
            var fechaVenc = doc.FechaExpiracion.Value;
            var diasParaVencer = (fechaVenc - hoy).Days;

            if (diasParaVencer <= dias)
            {
                resultado.Add(new DocumentoVencidoViewModel
                {
                    EmpleadoId = doc.EmpleadoId,
                    NombreCompleto = doc.Empleado?.NombreCompleto ?? "Sin empleado",
                    Departamento = doc.Empleado?.Departamento?.Nombre ?? "Sin asignar",
                    TipoDocumento = doc.Tipo.ToString(),
                    FechaVencimiento = fechaVenc,
                    DiasVencido = Math.Abs(diasParaVencer),
                    Estado = diasParaVencer < 0 ? "Vencido" : "Por Vencer"
                });
            }
        }

        return resultado
            .OrderBy(d => d.Estado == "Vencido" ? 0 : 1)
            .ThenBy(d => d.FechaVencimiento)
            .ToList();
    }

    // ════════════════════════════════════════════
    // PROGRAMACIÓN DE REPORTES
    // ════════════════════════════════════════════

    public async Task<List<ReporteProgramadoViewModel>> GetReportesProgramadosAsync()
    {
        var reportes = await _context.ReportesProgramados
            .Include(r => r.Departamento)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return reportes.Select(r => new ReporteProgramadoViewModel
        {
            Id = r.Id,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            TipoReporte = r.TipoReporte,
            Frecuencia = r.Frecuencia,
            DepartamentoId = r.DepartamentoId,
            EmailDestino = r.EmailDestino,
            EmailsCC = r.EmailsCC,
            HoraEnvio = r.HoraEnvio,
            DiaSemana = r.DiaSemana,
            DiaMes = r.DiaMes,
            Activo = r.Activo,
            UltimoEnvio = r.UltimoEnvio,
            ProximoEnvio = r.ProximoEnvio,
            IncluirExcel = r.IncluirExcel,
            IncluirPDF = r.IncluirPDF,
            EnviarAlertas = r.EnviarAlertas,
            UltimoError = r.UltimoError
        }).ToList();
    }

    public async Task<ReporteProgramadoViewModel?> GetReporteProgramadoByIdAsync(int id)
    {
        var r = await _context.ReportesProgramados
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (r == null) return null;

        return new ReporteProgramadoViewModel
        {
            Id = r.Id,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            TipoReporte = r.TipoReporte,
            Frecuencia = r.Frecuencia,
            DepartamentoId = r.DepartamentoId,
            EmailDestino = r.EmailDestino,
            EmailsCC = r.EmailsCC,
            HoraEnvio = r.HoraEnvio,
            DiaSemana = r.DiaSemana,
            DiaMes = r.DiaMes,
            Activo = r.Activo,
            UltimoEnvio = r.UltimoEnvio,
            ProximoEnvio = r.ProximoEnvio,
            IncluirExcel = r.IncluirExcel,
            IncluirPDF = r.IncluirPDF,
            EnviarAlertas = r.EnviarAlertas,
            UltimoError = r.UltimoError
        };
    }

    public async Task<ReporteProgramado> CrearReporteProgramadoAsync(CrearReporteProgramadoViewModel model)
    {
        var proximoEnvio = CalcularProximoEnvio(model.Frecuencia, model.DiaMes, model.DiaSemana, model.HoraEnvio);

        var entity = new ReporteProgramado
        {
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            TipoReporte = model.TipoReporte,
            Frecuencia = model.Frecuencia,
            DepartamentoId = model.DepartamentoId,
            EmailDestino = model.EmailDestino,
            EmailsCC = model.EmailsCC,
            HoraEnvio = model.HoraEnvio,
            DiaSemana = model.DiaSemana,
            DiaMes = model.DiaMes,
            ProximoEnvio = proximoEnvio,
            IncluirExcel = model.IncluirExcel,
            IncluirPDF = model.IncluirPDF,
            EnviarAlertas = model.EnviarAlertas,
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReportesProgramados.Add(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<bool> ActualizarReporteProgramadoAsync(int id, CrearReporteProgramadoViewModel model)
    {
        var entity = await _context.ReportesProgramados.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.Nombre = model.Nombre;
        entity.Descripcion = model.Descripcion;
        entity.TipoReporte = model.TipoReporte;
        entity.Frecuencia = model.Frecuencia;
        entity.DepartamentoId = model.DepartamentoId;
        entity.EmailDestino = model.EmailDestino;
        entity.EmailsCC = model.EmailsCC;
        entity.HoraEnvio = model.HoraEnvio;
        entity.DiaSemana = model.DiaSemana;
        entity.DiaMes = model.DiaMes;
        entity.ProximoEnvio = CalcularProximoEnvio(model.Frecuencia, model.DiaMes, model.DiaSemana, model.HoraEnvio);
        entity.IncluirExcel = model.IncluirExcel;
        entity.IncluirPDF = model.IncluirPDF;
        entity.EnviarAlertas = model.EnviarAlertas;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarReporteProgramadoAsync(int id)
    {
        var entity = await _context.ReportesProgramados.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivarDesactivarReporteProgramadoAsync(int id, bool activo)
    {
        var entity = await _context.ReportesProgramados.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.Activo = activo;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ReporteProgramado>> GetReportesPorEnviarAsync()
    {
        var ahora = DateTime.Now;
        return await _context.ReportesProgramados
            .Where(r => !r.IsDeleted && r.Activo && r.ProximoEnvio <= ahora)
            .ToListAsync();
    }

    public async Task MarcarEnvioExitosoAsync(int id, DateTime fecha)
    {
        var entity = await _context.ReportesProgramados.FindAsync(id);
        if (entity == null) return;

        entity.UltimoEnvio = fecha;
        entity.UltimoError = null;
        entity.FechaUltimaGeneracion = fecha;
        entity.ProximoEnvio = CalcularProximoEnvio(entity.Frecuencia, entity.DiaMes, entity.DiaSemana, entity.HoraEnvio ?? new TimeSpan(8, 0, 0));
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task MarcarEnvioFallidoAsync(int id, string error)
    {
        var entity = await _context.ReportesProgramados.FindAsync(id);
        if (entity == null) return;

        entity.UltimoError = error;
        entity.ProximoEnvio = DateTime.Now.AddHours(1);
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<byte[]> GenerarExcelReporteAsync(string tipoReporte, int? departamentoId = null)
    {
        var data = new List<Dictionary<string, object>>();
        var titulo = "";

        switch (tipoReporte)
        {
            case "PlanillaMensual":
                var planilla = await GetPlanillaMensualAsync(DateTime.Now.Month, DateTime.Now.Year, departamentoId);
                titulo = "Planilla Mensual";
                data = planilla.Select(p => new Dictionary<string, object>
                {
                    ["Código"] = p.Codigo,
                    ["Nombre"] = p.NombreCompleto,
                    ["Departamento"] = p.Departamento,
                    ["Puesto"] = p.Puesto,
                    ["Salario Base"] = p.SalarioBase,
                    ["Horas Extra"] = p.HorasExtra,
                    ["Bonificación"] = p.Bonificacion250,
                    ["Total Devengado"] = p.TotalDevengado,
                    ["IGSS Laboral"] = p.IGSSLaboral,
                    ["ISR"] = p.ISR,
                    ["Total Deducciones"] = p.TotalDeducciones,
                    ["Salario Neto"] = p.SalarioNeto,
                    ["IGSS Patronal"] = p.IGSSPatronal,
                    ["Costo Total"] = p.CostoTotal
                }).ToList();
                break;

            case "Bono14Aguinaldo":
                var bono = await GetBono14AguinaldoProyectadoAsync(departamentoId);
                titulo = "Bono 14 y Aguinaldo Proyectado";
                data = bono.Select(b => new Dictionary<string, object>
                {
                    ["Nombre"] = b.NombreCompleto,
                    ["Departamento"] = b.Departamento,
                    ["Fecha Ingreso"] = b.FechaIngreso.ToString("dd/MM/yyyy"),
                    ["Salario Base"] = b.SalarioBase,
                    ["Bono 14"] = b.Bono14Proyectado,
                    ["Aguinaldo"] = b.AguinaldoProyectado,
                    ["Total Prestaciones"] = b.TotalPrestaciones,
                    ["Meses Trabajados"] = b.MesesTrabajados
                }).ToList();
                break;

            case "ContratosVencer":
                var contratos = await GetContratosPorVencerAsync(90);
                titulo = "Contratos por Vencer";
                data = contratos.Select(c => new Dictionary<string, object>
                {
                    ["Nombre"] = c.NombreCompleto,
                    ["Departamento"] = c.Departamento,
                    ["Puesto"] = c.Puesto,
                    ["Tipo Contrato"] = c.TipoContrato.ToString(),
                    ["Fecha Ingreso"] = c.FechaIngreso.ToString("dd/MM/yyyy"),
                    ["Fecha Vencimiento"] = c.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "N/A",
                    ["Días para Vencer"] = c.DiasParaVencer,
                    ["Alerta"] = c.Alerta
                }).ToList();
                break;

            case "DocumentosVencidos":
                var docs = await GetDocumentosVencidosOPorVencerAsync(30);
                titulo = "Documentos Vencidos/Por Vencer";
                data = docs.Select(d => new Dictionary<string, object>
                {
                    ["Empleado"] = d.NombreCompleto,
                    ["Departamento"] = d.Departamento,
                    ["Tipo Documento"] = d.TipoDocumento,
                    ["Fecha Vencimiento"] = d.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "N/A",
                    ["Días"] = d.DiasVencido,
                    ["Estado"] = d.Estado
                }).ToList();
                break;

            default:
                titulo = "Reporte General";
                break;
        }

        return ExportToExcel(data, titulo);
    }

    private byte[] ExportToExcel(List<Dictionary<string, object>> data, string titulo)
    {
        using var memoryStream = new MemoryStream();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add(titulo);

        if (data.Count == 0)
        {
            worksheet.Cell(1, 1).Value = "No hay datos";
            return memoryStream.ToArray();
        }

        var headers = data[0].Keys.ToList();
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0d9488");
            worksheet.Cell(1, i + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        }

        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < headers.Count; col++)
            {
                var value = data[row][headers[col]];
                worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
            }
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private DateTime CalcularProximoEnvio(FrecuenciaProgramacion frecuencia, int? diaMes, DayOfWeek? diaSemana, TimeSpan hora)
    {
        var ahora = DateTime.Now;
        var proximo = new DateTime(ahora.Year, ahora.Month, ahora.Day, hora.Hours, hora.Minutes, 0);

        switch (frecuencia)
        {
            case FrecuenciaProgramacion.Semanal:
                if (diaSemana.HasValue)
                {
                    var diasFaltantes = ((int)diaSemana.Value - (int)ahora.DayOfWeek + 7) % 7;
                    if (diasFaltantes == 0) diasFaltantes = 7;
                    proximo = ahora.AddDays(diasFaltantes);
                }
                else
                {
                    proximo = proximo.AddDays(7);
                }
                break;

            case FrecuenciaProgramacion.Mensual:
                var dia = diaMes ?? 1;
                if (ahora.Day <= dia)
                {
                    proximo = new DateTime(ahora.Year, ahora.Month, dia, hora.Hours, hora.Minutes, 0);
                }
                else
                {
                    proximo = new DateTime(ahora.Year, ahora.Month + 1, dia, hora.Hours, hora.Minutes, 0);
                }
                break;

            case FrecuenciaProgramacion.Trimestral:
                var mesActual = ahora.Month;
                var mesInicio = ((mesActual - 1) / 3 + 1) * 3 + 1;
                if (mesInicio > 12)
                {
                    proximo = new DateTime(ahora.Year + 1, mesInicio - 12, diaMes ?? 1, hora.Hours, hora.Minutes, 0);
                }
                else
                {
                    proximo = new DateTime(ahora.Year, mesInicio, diaMes ?? 1, hora.Hours, hora.Minutes, 0);
                }
                break;
        }

        return proximo;
    }
}
