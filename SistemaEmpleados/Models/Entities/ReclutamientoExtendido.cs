using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

// ════════════════════════════════════════════
// HISTORIAL DE ESTADOS DE PLAZA
// Registra cada cambio de estado con motivo
// ════════════════════════════════════════════
public class HistorialEstadoPlaza : BaseEntity
{
    public int PlazaVacanteId { get; set; }
    public PlazaVacante PlazaVacante { get; set; } = null!;

    public EstadoPlaza EstadoAnterior { get; set; }
    public EstadoPlaza EstadoNuevo { get; set; }

    public string? Motivo { get; set; }
    public string CambiadoPor { get; set; } = string.Empty;
    public DateTime FechaCambio { get; set; } = DateTime.Now;
}

// ════════════════════════════════════════════
// ENTREVISTA — registro detallado
// ════════════════════════════════════════════
public enum ResultadoEntrevista
{
    Pendiente,
    Aprobado,
    Reprobado,
    Postergado
}

public class Entrevista : BaseEntity
{
    public int CandidatoId { get; set; }
    public Candidato Candidato { get; set; } = null!;

    public DateTime FechaHora { get; set; }
    public string Entrevistador { get; set; } = string.Empty;
    public string? Lugar { get; set; }

    // Resultado
    public ResultadoEntrevista Resultado { get; set; }
        = ResultadoEntrevista.Pendiente;

    [Column(TypeName = "decimal(3,1)")]
    public decimal Calificacion { get; set; } = 0; // 1-10

    public string? Observaciones { get; set; }
    public bool Notificado { get; set; } = false;
}

// ════════════════════════════════════════════
// NOTA / SEGUIMIENTO de candidato
// ════════════════════════════════════════════
public class NotaCandidato : BaseEntity
{
    public int CandidatoId { get; set; }
    public Candidato Candidato { get; set; } = null!;

    public string Nota { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
}