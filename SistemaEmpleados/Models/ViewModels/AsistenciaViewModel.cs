using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

public class AsistenciaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    [Display(Name = "Empleado")]
    public int EmpleadoId { get; set; }

    public string? NombreEmpleado { get; set; }

    [Display(Name = "Horario")]
    public int? HorarioId { get; set; }

    [Required(ErrorMessage = "La fecha es requerida")]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Display(Name = "Hora de entrada")]
    public string? HoraEntrada { get; set; } // ✅ FIX — faltaba el } de cierre

    [Display(Name = "Hora de salida")]
    public string? HoraSalida { get; set; }

    [Range(0, 24)]
    [Display(Name = "Horas extra")]
    public decimal HorasExtra { get; set; } = 0;

    [Range(0, 999)]
    [Display(Name = "Minutos de atraso")]
    public int MinutosAtraso { get; set; } = 0;

    [Display(Name = "Método")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MetodoMarcaje Metodo { get; set; } = MetodoMarcaje.Manual;

    [Display(Name = "Estado")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EstadoAsistencia Estado { get; set; } = EstadoAsistencia.Presente;

    [Display(Name = "Observación")]
    public string? Observacion { get; set; }
}

public class AsistenciaListViewModel
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string? HoraEntrada { get; set; }
    public string? HoraSalida { get; set; }
    public decimal HorasExtra { get; set; }
    public int MinutosAtraso { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal HorasTrabajadas { get; set; }
}

public class HorarioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La hora de entrada es requerida")]
    [Display(Name = "Hora de entrada")]
    public string HoraEntrada { get; set; } = "08:00";

    [Required(ErrorMessage = "La hora de salida es requerida")]
    [Display(Name = "Hora de salida")]
    public string HoraSalida { get; set; } = "17:00";

    [Display(Name = "Minutos tolerancia tardanza")]
    public int MinutosToleranciaTardanza { get; set; } = 15;

    public bool Activo { get; set; } = true;
}