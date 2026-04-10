using System.ComponentModel.DataAnnotations;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

public class VacacionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    public int EmpleadoId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es requerida")]
    [Display(Name = "Fecha inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de fin es requerida")]
    [Display(Name = "Fecha fin")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Observación")]
    public string? Observacion { get; set; }

    public EstadoVacacion Estado { get; set; } = EstadoVacacion.Pendiente;
}

public class VacacionListViewModel
{
    public int Id { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
    public int DiasHabiles { get; set; }
    public int DiasSolicitados { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FechaSolicitud { get; set; } = string.Empty;
    public string? AprobadoPor { get; set; }
}

public class AusenciaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    public int EmpleadoId { get; set; }

    [Display(Name = "Tipo de ausencia")]
    public TipoAusencia Tipo { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es requerida")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de fin es requerida")]
    public DateTime FechaFin { get; set; } = DateTime.Today;

    public bool Justificada { get; set; } = false;
    public string? Documento { get; set; }
    public string? Observacion { get; set; }
}

public class AusenciaListViewModel
{
    public int Id { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
    public int TotalDias { get; set; }
    public bool Justificada { get; set; }
    public string? Observacion { get; set; }
}

public class SaldoVacacionViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Antiguedad { get; set; } = string.Empty;
    public int DiasCorresponden { get; set; }
    public int DiasTomados { get; set; }
    public int DiasPendientes { get; set; }
    public int DiasDisponibles { get; set; }
}

public class VacacionKpiViewModel
{
    public int TotalSolicitudes { get; set; }
    public int Pendientes { get; set; }
    public int Aprobadas { get; set; }
    public int EnVacacionesHoy { get; set; }
}