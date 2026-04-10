using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

// ✅ FIX — agrega Tarjeta y App que el JS usa pero el enum no tenía
public enum MetodoMarcaje { Manual, Biometrico, QR, Tarjeta, App }
public enum EstadoAsistencia { Presente, Ausente, Tardanza, PermisoJustificado }

public class Horario : BaseEntity
{
    [Required] public string Nombre { get; set; } = string.Empty;
    public TimeSpan HoraEntrada { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public int MinutosToleranciaTardanza { get; set; } = 15;
    public bool Activo { get; set; } = true;
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}

public class Asistencia : BaseEntity
{
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    public int? HorarioId { get; set; }
    public Horario? Horario { get; set; }

    public DateTime Fecha { get; set; }
    public TimeSpan? HoraEntrada { get; set; }
    public TimeSpan? HoraSalida { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal HorasExtra { get; set; } = 0;

    public int MinutosAtraso { get; set; } = 0;
    public MetodoMarcaje Metodo { get; set; } = MetodoMarcaje.Manual;
    public EstadoAsistencia Estado { get; set; } = EstadoAsistencia.Presente;
    public string? Observacion { get; set; }

    // ✅ FIX TURNO NOCTURNO — suma 24h si salida < entrada (ej: 20:00 → 02:00)
    [NotMapped]
    public decimal HorasTrabajadas =>
        HoraEntrada.HasValue && HoraSalida.HasValue
            ? HoraSalida.Value >= HoraEntrada.Value
                ? (decimal)(HoraSalida.Value - HoraEntrada.Value).TotalHours
                : (decimal)(HoraSalida.Value - HoraEntrada.Value + TimeSpan.FromHours(24)).TotalHours
            : 0;
}