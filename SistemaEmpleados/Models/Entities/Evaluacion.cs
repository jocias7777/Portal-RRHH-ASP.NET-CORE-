using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum TipoEvaluacion { Noventa = 90, CientoOchenta = 180, Trescientos60 = 360 }
public enum EstadoEvaluacion { Pendiente, EnProceso, Completada, Cancelada }

public class KPI : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Peso { get; set; } // % del total

    public int? PuestoId { get; set; }
    public Puesto? Puesto { get; set; }
    public bool Activo { get; set; } = true;
}

public class Evaluacion : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public string EvaluadorId { get; set; } = string.Empty;
    public ApplicationUser Evaluador { get; set; } = null!;

    public string Periodo { get; set; } = string.Empty; // "2024-Q1"
    public TipoEvaluacion TipoEvaluacion { get; set; }
    public EstadoEvaluacion Estado { get; set; } = EstadoEvaluacion.Pendiente;
    public DateTime FechaEvaluacion { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(5,2)")]
    public decimal PuntajeTotal { get; set; } = 0;

    public string? Comentarios { get; set; }
    public string? PlanMejora { get; set; }

    public ICollection<ResultadoKPI> Resultados { get; set; } = new List<ResultadoKPI>();
}

public class ResultadoKPI : BaseEntity
{
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;

    public int KPIId { get; set; }
    public KPI KPI { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Calificacion { get; set; } // 0-100

    [Column(TypeName = "decimal(5,2)")]
    public decimal PuntajePonderado { get; set; } // Calificacion * Peso / 100

    public string? Observacion { get; set; }
}
