using System.ComponentModel.DataAnnotations;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

public class PrestacionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    public int EmpleadoId { get; set; }

    [Display(Name = "Tipo de prestación")]
    public TipoPrestacion Tipo { get; set; }

    [Required(ErrorMessage = "El período es requerido")]
    [Display(Name = "Período (año)")]
    public int Periodo { get; set; } = DateTime.Now.Year;

    [Display(Name = "Meses trabajados")]
    public int MesesTrabajados { get; set; }

    [Display(Name = "Salario base")]
    public decimal SalarioBase { get; set; }

    [Display(Name = "Monto calculado")]
    public decimal Monto { get; set; }

    public EstadoPrestacion Estado { get; set; } = EstadoPrestacion.Pendiente;

    [Display(Name = "Fecha de pago")]
    public DateTime? FechaPago { get; set; }

    public string? Observacion { get; set; }
}

public class PrestacionListViewModel
{
    public int Id { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Periodo { get; set; }
    public int MesesTrabajados { get; set; }
    public decimal SalarioBase { get; set; }
    public decimal Monto { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? FechaPago { get; set; }
}

public class MarcarTodasViewModel
{
    public string? Tipo { get; set; }
    public int? Periodo { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.Today;
}

public class GenerarPrestacionesAnioViewModel
{
    public int Anio { get; set; }
    public int? DepartamentoId { get; set; }
}



// Reemplaza la clase CalculoPrestacionViewModel existente completa
public class CalculoPrestacionViewModel
{
    public int EmpleadoId { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public decimal SalarioBase { get; set; }
    public decimal SalarioPromedio { get; set; }
    public DateTime FechaIngreso { get; set; }
    public int MesesTrabajados { get; set; }
    public int AniosTrabajados { get; set; }

    // ── Nuevos campos de detalle ──
    public int DiasEfectivos { get; set; }
    public int DiasVacacionesTomados { get; set; }
    public int DiasAusencias { get; set; }
    public bool HuboambioSalario { get; set; }

    public decimal Aguinaldo { get; set; }
    public decimal Bono14 { get; set; }
    public decimal Indemnizacion { get; set; }
    public decimal TotalFiniquito { get; set; }
}
