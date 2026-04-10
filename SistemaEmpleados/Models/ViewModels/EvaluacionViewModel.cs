using System.ComponentModel.DataAnnotations;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

public class KPIViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    [Range(0.01, 100, ErrorMessage = "El peso debe estar entre 0.01 y 100")]
    [Display(Name = "Peso (%)")]
    public decimal Peso { get; set; }

    public int? PuestoId { get; set; }
    public bool Activo { get; set; } = true;
}

public class KPIListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Peso { get; set; }
    public string? Puesto { get; set; }
    public bool Activo { get; set; }
}

public class EvaluacionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    public int EmpleadoId { get; set; }

    [Required(ErrorMessage = "El período es requerido")]
    public string Periodo { get; set; } = $"{DateTime.Now.Year}-Q{(DateTime.Now.Month - 1) / 3 + 1}";

    public TipoEvaluacion TipoEvaluacion { get; set; } = TipoEvaluacion.CientoOchenta;
    public EstadoEvaluacion Estado { get; set; } = EstadoEvaluacion.Pendiente;

    [Display(Name = "Fecha de evaluación")]
    public DateTime FechaEvaluacion { get; set; } = DateTime.Today;

    public string? Comentarios { get; set; }
    public string? PlanMejora { get; set; }
    public List<ResultadoKPIViewModel> Resultados { get; set; } = new();
}

public class ResultadoKPIViewModel
{
    public int Id { get; set; }
    public int KPIId { get; set; }
    public string NombreKPI { get; set; } = string.Empty;
    public decimal PesoKPI { get; set; }

    [Range(0, 100, ErrorMessage = "La calificación debe ser entre 0 y 100")]
    public decimal Calificacion { get; set; } = 0;

    public string? Observacion { get; set; }
}

public class EvaluacionListViewModel
{
    public int Id { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public string NombreEvaluador { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string TipoEvaluacion { get; set; } = string.Empty;
    public decimal PuntajeTotal { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FechaEvaluacion { get; set; } = string.Empty;
    public string Calificacion { get; set; } = string.Empty;
}
