using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum TipoPrestacion { Aguinaldo, Bono14, Indemnizacion, Finiquito, Otro }
public enum EstadoPrestacion { Pendiente, Calculado, Pagado, Cancelado }

public class Prestacion : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public TipoPrestacion Tipo { get; set; }
    public int Periodo { get; set; } // año
    public int MesesTrabajados { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioBase { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    public EstadoPrestacion Estado { get; set; } = EstadoPrestacion.Pendiente;

    public DateTime? FechaPago { get; set; }
    public string? Observacion { get; set; }
    public string? CalculadoPor { get; set; }
}