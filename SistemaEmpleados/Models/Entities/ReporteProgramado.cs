using System.ComponentModel.DataAnnotations;

namespace SistemaEmpleados.Models.Entities;

public enum TipoReporteProgramado
{
    PlanillaMensual,
    Bono14Aguinaldo,
    ContratosVencer,
    DocumentosVencidos,
    ResumenGeneral
}

public enum FrecuenciaProgramacion
{
    Mensual,
    Semanal,
    Trimestral
}

public class ReporteProgramado : BaseEntity
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public TipoReporteProgramado TipoReporte { get; set; }

    [Required]
    public FrecuenciaProgramacion Frecuencia { get; set; } = FrecuenciaProgramacion.Mensual;

    public int? DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }

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

    public DateTime? FechaUltimaGeneracion { get; set; }

    public bool IncluirExcel { get; set; } = true;

    public bool IncluirPDF { get; set; } = false;

    public bool EnviarAlertas { get; set; } = true;

    public string? UltimoError { get; set; }

    public DateTime? CreatedBy { get; set; }

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