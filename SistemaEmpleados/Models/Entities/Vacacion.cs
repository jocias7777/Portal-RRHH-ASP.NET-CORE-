using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum EstadoVacacion { Pendiente, Aprobado, Rechazado, Cancelado }
public enum TipoAusencia { Enfermedad, Personal, Maternidad, Paternidad, Duelo, Otro }

public class Vacacion : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public int DiasHabiles { get; set; }
    public int DiasSolicitados { get; set; }

    public EstadoVacacion Estado { get; set; } = EstadoVacacion.Pendiente;

    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Today;
    public string? Observacion { get; set; }
}

public class Ausencia : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public TipoAusencia Tipo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int TotalDias { get; set; }

    public bool Justificada { get; set; } = false;
    public string? Documento { get; set; }
    public string? Observacion { get; set; }
}
