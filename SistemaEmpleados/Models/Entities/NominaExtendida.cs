using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

// ════════════════════════════════════════════
// HISTORIAL DE SALARIOS
// Registra cada cambio de salario de un empleado
// ════════════════════════════════════════════




// ════════════════════════════════════════════
// CONCEPTO DE NÓMINA
// Catálogo de bonos y deducciones configurables
// ════════════════════════════════════════════
public enum TipoConcepto { Devengado, Deduccion }
public enum AplicacionConcepto { Porcentaje, MontoFijo, Formula }

public class ConceptoNomina : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public TipoConcepto Tipo { get; set; }
    public AplicacionConcepto Aplicacion { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal Valor { get; set; }

    // Si es true, se aplica a todos los empleados automáticamente
    public bool EsObligatorio { get; set; } = false;

    // Si es true, es un concepto del sistema (no se puede eliminar)
    public bool EsSistema { get; set; } = false;

    public bool Activo { get; set; } = true;
    public string? Observacion { get; set; }
}


// ════════════════════════════════════════════
// PRÉSTAMO EMPLEADO
// Control de préstamos con cuotas mensuales
// ════════════════════════════════════════════
public enum EstadoPrestamo { Activo, Completado, Cancelado }

public class PrestamoEmpleado : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaMensual { get; set; }

    public int NumeroCuotas { get; set; }
    public int CuotasPagadas { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoPendiente { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinEstimada { get; set; }

    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
    public string? Motivo { get; set; }
    public string AutorizadoPor { get; set; } = string.Empty;
}

