using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum TipoDocumento { Contrato, Rol, Certificado, Constancia, Credencial, Antecedentes, Otro }
public enum EstadoDocumento { Activo, Expirado, Archivado, PendienteFirma }
public enum ModalidadContrato { TiempoIndefinido, TiempoDeterminado, ObraDeterminada }

public class Documento : BaseEntity
{
    [Required]
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public TipoDocumento Tipo { get; set; }

    public ModalidadContrato? Modalidad { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Activo;

    public string UrlArchivo { get; set; } = string.Empty;

    public string? NombreArchivo { get; set; }

    public string? ContentType { get; set; }

    public long? TamanioBytes { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public string? NumeroFolio { get; set; }

    public string? UrlExterna { get; set; }

    public string? Observaciones { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;
}