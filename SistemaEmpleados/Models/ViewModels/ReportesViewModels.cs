using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

// ════════════════════════════════════════════
// FILTROS COMUNES PARA REPORTES
// ════════════════════════════════════════════

public class FiltrosReporteViewModel
{
    [Display(Name = "Fecha desde")]
    public DateTime FechaDesde { get; set; } = DateTime.Today.AddMonths(-1);

    [Display(Name = "Fecha hasta")]
    public DateTime FechaHasta { get; set; } = DateTime.Today;

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    [Display(Name = "Empleado")]
    public int? EmpleadoId { get; set; }

    [Display(Name = "Tipo de reporte")]
    public string? TipoReporte { get; set; }
}

// ════════════════════════════════════════════
// NÓMINA Y COSTOS LABORALES
// ════════════════════════════════════════════

public class ReportePlanillaMensualViewModel
{
    public int EmpleadoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioBase { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HorasExtra { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bonificacion250 { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDevengado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IGSSLaboral { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ISR { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeducciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioNeto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IGSSPatronal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoTotal { get; set; }
}

public class ResumenBono14AguinaldoViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioBase { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bono14Proyectado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AguinaldoProyectado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrestaciones { get; set; }

    public int MesesTrabajados { get; set; }
}

public class CostoPorDepartamentoViewModel
{
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int CantidadEmpleados { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalarios { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalIGSSPatronal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBono14 { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAguinaldo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrestaciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoTotalDepartamento { get; set; }
}

// ════════════════════════════════════════════
// CUMPLIMIENTO LEGAL
// ════════════════════════════════════════════

public class ContratoPorVencerViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public TipoContrato TipoContrato { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public int DiasParaVencer { get; set; }
    public string Alerta { get; set; } = string.Empty; // "30 días", "60 días", "90 días"
}

public class EmpleadoSinDocumentosViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool TieneCUI { get; set; }
    public bool TieneIGSS { get; set; }
    public bool TieneNIT { get; set; }
    public bool TieneAntecedentes { get; set; }
    public List<string> DocumentosFaltantes { get; set; } = new();
}

public class ReporteNacionalidadViewModel
{
    public int TotalEmpleados { get; set; }
    public int EmpleadosGuatemaltecos { get; set; }
    public int EmpleadosExtranjeros { get; set; }
    public decimal PorcentajeGuatemaltecos { get; set; }
    public decimal PorcentajeExtranjeros { get; set; }
    public bool CumpleArticulo14 { get; set; } // Límite 10% extranjeros
    public string Observacion { get; set; } = string.Empty;
}

public class VacacionesNoTomadasViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public int DiasDisponibles { get; set; }
    public int DiasTomadosAnioActual { get; set; }
    public DateTime? UltimaVacacionFecha { get; set; }
    public bool TieneDerecho { get; set; } // Más de 1 año
}

// ════════════════════════════════════════════
// ASISTENCIA Y TIEMPO
// ════════════════════════════════════════════

public class ReporteInasistenciasTardanzasViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;

    public int TotalDiasPeriodo { get; set; }
    public int DiasPresentes { get; set; }
    public int DiasAusentes { get; set; }
    public int DiasTardanza { get; set; }
    public int DiasJustificados { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PorcentajeAsistencia { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalHorasExtra { get; set; }

    public string Estado { get; set; } = string.Empty; // "Normal", "Riesgo"
}

public class HorasExtraPorEmpleadoViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalHorasExtraMes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoHorasExtra { get; set; }

    public int CantidadDiasConHoraExtra { get; set; }
}

public class EmpleadoConMasDeTresFaltasViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int TotalFaltasMes { get; set; }
    public List<DateTime> FechasFalta { get; set; } = new();
    public bool EsReincidente { get; set; }
}

// ════════════════════════════════════════════
// ROTACIÓN DE PERSONAL
// ════════════════════════════════════════════

public class AltasBajasViewModel
{
    public DateTime Periodo { get; set; } // Mes/Año
    public int Altas { get; set; }
    public int Bajas { get; set; }
    public int Neto { get; set; }
    public decimal TasaRotacion { get; set; } // (Bajas / Promedio Empleados) * 100
}

public class MotivoSalidaViewModel
{
    public string Motivo { get; set; } = string.Empty; // Renuncia, Despido, Mutuo Acuerdo
    public int Cantidad { get; set; }
    public decimal Porcentaje { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalIndemnizaciones { get; set; }
}

public class TiempoPermanenciaViewModel
{
    public string Departamento { get; set; } = string.Empty;
    public int CantidadEmpleados { get; set; }
    public decimal TiempoPromedioAnios { get; set; }
    public decimal TiempoPromedioMeses { get; set; }
    public EmpleadoAntiguedadRango RangoAntiguedad { get; set; }
}

public class EmpleadoAntiguedadRango
{
    public int Menos1Anio { get; set; }
    public int De1a3Anios { get; set; }
    public int De3a5Anios { get; set; }
    public int MasDe5Anios { get; set; }
}

// ════════════════════════════════════════════
// PRESTACIONES E INDEMNIZACIONES
// ════════════════════════════════════════════

public class ProyeccionIndemnizacionViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public decimal SalarioBase { get; set; }
    public decimal SalarioPromedio6Meses { get; set; }
    public int AniosServicio { get; set; }
    public int MesesAdicionales { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IndemnizacionPorAnios { get; set; } // 1 salario por año

    [Column(TypeName = "decimal(18,2)")]
    public decimal IndemnizacionProporcional { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VacacionesNoGozadas { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AguinaldoProporcional { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bono14Proporcional { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalIndemnificacion { get; set; }
}

public class VacacionesAcumuladasViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public int DiasAcumulados { get; set; }
    public int DiasTomadosAnio { get; set; }
    public decimal ValorDiario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorQuetzales { get; set; }
}

public class FiniquitoEmitidoViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime FechaSalida { get; set; }
    public string MotivoSalida { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioDevengado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VacacionesNoGozadas { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AguinaldoProporcional { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bono14Proporcional { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeducciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalNetoPagar { get; set; }

    public DateTime FechaEmision { get; set; }
}

// ════════════════════════════════════════════
// EXPEDIENTES
// ════════════════════════════════════════════

public class CompletitudExpedienteViewModel
{
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int TotalEmpleados { get; set; }
    public int ExpedientesCompletos { get; set; }
    public int ExpedientesIncompletos { get; set; }
    public decimal PorcentajeCompletitud { get; set; }
    public List<string> EmpleadosSinExpedienteCompleto { get; set; } = new();
}

public class DocumentoVencidoViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public DateTime? FechaVencimiento { get; set; }
    public int DiasVencido { get; set; }
    public string Estado { get; set; } = string.Empty; // "Vencido", "Por Vencer"
}

// ════════════════════════════════════════════
// RESUMEN GENERAL PARA EXPORTACIÓN
// ════════════════════════════════════════════

public class ReporteExportableViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Datos { get; set; } = new();
    public Dictionary<string, decimal> Totales { get; set; } = new();
    public string Notas { get; set; } = string.Empty;
}

// ════════════════════════════════════════════
// PROGRAMACIÓN DE REPORTES AUTOMÁTICOS
// ════════════════════════════════════════════

public class ReporteProgramadoViewModel
{
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public TipoReporteProgramado TipoReporte { get; set; }

    [Required]
    public FrecuenciaProgramacion Frecuencia { get; set; }

    public int? DepartamentoId { get; set; }

    [Required]
    [EmailAddress]
    public string EmailDestino { get; set; } = string.Empty;

    public string? EmailsCC { get; set; }

    public TimeSpan? HoraEnvio { get; set; }

    public DayOfWeek? DiaSemana { get; set; }

    public int? DiaMes { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? UltimoEnvio { get; set; }

    public DateTime? ProximoEnvio { get; set; }

    public bool IncluirExcel { get; set; } = true;

    public bool IncluirPDF { get; set; } = false;

    public bool EnviarAlertas { get; set; } = true;

    public string? UltimoError { get; set; }

    public string TipoReporteNombre => TipoReporte switch
    {
        TipoReporteProgramado.PlanillaMensual => "Planilla Mensual",
        TipoReporteProgramado.Bono14Aguinaldo => "Bono 14 y Aguinaldo",
        TipoReporteProgramado.ContratosVencer => "Contratos por Vencer",
        TipoReporteProgramado.DocumentosVencidos => "Documentos Vencidos",
        TipoReporteProgramado.ResumenGeneral => "Resumen General",
        _ => TipoReporte.ToString()
    };

    public string FrecuenciaNombre => Frecuencia switch
    {
        FrecuenciaProgramacion.Mensual => "Mensual",
        FrecuenciaProgramacion.Semanal => "Semanal",
        FrecuenciaProgramacion.Trimestral => "Trimestral",
        _ => Frecuencia.ToString()
    };
}

public class CrearReporteProgramadoViewModel
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public TipoReporteProgramado TipoReporte { get; set; }

    [Required]
    public FrecuenciaProgramacion Frecuencia { get; set; } = FrecuenciaProgramacion.Mensual;

    public int? DepartamentoId { get; set; }

    [Required]
    [EmailAddress]
    public string EmailDestino { get; set; } = string.Empty;

    public string? EmailsCC { get; set; }

    public TimeSpan HoraEnvio { get; set; } = new TimeSpan(8, 0, 0);

    public DayOfWeek? DiaSemana { get; set; }

    public int? DiaMes { get; set; } = 1;

    public bool IncluirExcel { get; set; } = true;

    public bool IncluirPDF { get; set; } = false;

    public bool EnviarAlertas { get; set; } = true;
}
