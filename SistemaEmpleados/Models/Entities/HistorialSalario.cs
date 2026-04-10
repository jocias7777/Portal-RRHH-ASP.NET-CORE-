using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public class HistorialSalario : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioAnterior { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioNuevo { get; set; }

    public DateTime FechaCambio { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? CambiadoPor { get; set; }
}