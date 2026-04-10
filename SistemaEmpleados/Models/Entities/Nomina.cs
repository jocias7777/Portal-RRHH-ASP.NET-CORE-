using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum EstadoPlanilla { Borrador, Procesada, Pagada, Anulada }

public class Planilla : BaseEntity
{
    public int Periodo { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public EstadoPlanilla Estado { get; set; } = EstadoPlanilla.Borrador;
    public string? GeneradoPor { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public DateTime? FechaPago { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDevengado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeducciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalNeto { get; set; }

    public ICollection<DetallePlanilla> Detalles { get; set; } = new List<DetallePlanilla>();
}

public class DetallePlanilla : BaseEntity
{
    public int PlanillaId { get; set; }
    public Planilla Planilla { get; set; } = null!;

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioBase { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HorasExtraMonto { get; set; } = 0;

    // Bonificación incentivo obligatoria Guatemala Q250
    [Column(TypeName = "decimal(18,2)")]
    public decimal Bonificacion250 { get; set; } = 250;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtrosBonos { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDevengado { get; set; }

    // Deducciones
    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaIGSS { get; set; }      // 4.83%

    [Column(TypeName = "decimal(18,2)")]
    public decimal ISR { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtrasDeducciones { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeducciones { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioNeto { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaIGSSPatronal { get; set; } = 0; // 12.67% cargo empresa

    [Column(TypeName = "decimal(18,2)")]
    public decimal Bono14 { get; set; } = 0; // Solo en julio

    [Column(TypeName = "decimal(18,2)")]
    public decimal Aguinaldo { get; set; } = 0; // Solo en diciembre

    [Column(TypeName = "decimal(18,2)")]
    public decimal DescuentoPrestamo { get; set; } = 0; // Cuota préstamo activo

    public string? Observacion { get; set; }
}