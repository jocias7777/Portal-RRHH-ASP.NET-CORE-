using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum EstadoEmpleado { Activo, Inactivo, Suspendido, Baja }
public enum Genero { Masculino, Femenino, Otro }
public enum TipoContrato { Indefinido, Temporal, PorObra, Prueba }

public class Empleado : BaseEntity
{
    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    public string PrimerNombre { get; set; } = string.Empty;

    public string? SegundoNombre { get; set; }

    [Required]
    public string PrimerApellido { get; set; } = string.Empty;

    public string? SegundoApellido { get; set; }

    public DateTime FechaNacimiento { get; set; }
    public Genero Genero { get; set; }

    // Documentos Guatemala
    [Required]
    public string CUI { get; set; } = string.Empty; // DPI

    public string? NIT { get; set; }
    public string? NumeroIGSS { get; set; }
    public string? NumeroIRTRA { get; set; }

    // Contacto
    public string? FotoUrl { get; set; }
    public string? Telefono { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    // Laboral
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaSalida { get; set; }

    public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.Activo;
    public TipoContrato TipoContrato { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioBase { get; set; }

    public string? Observaciones { get; set; }

  
    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int PuestoId { get; set; }
    public Puesto Puesto { get; set; } = null!;

    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

  
    [NotMapped]
    public string NombreCompleto =>
        $"{PrimerNombre} {SegundoNombre} {PrimerApellido} {SegundoApellido}"
            .Replace("  ", " ")
            .Trim();

    [NotMapped]
    public string Iniciales =>
        $"{(PrimerNombre?.FirstOrDefault() ?? 'X')}{(PrimerApellido?.FirstOrDefault() ?? 'X')}"
            .ToString()
            .ToUpper();
}