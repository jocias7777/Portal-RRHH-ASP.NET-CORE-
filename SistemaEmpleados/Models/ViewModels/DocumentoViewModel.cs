using SistemaEmpleados.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpleados.Models.ViewModels;

public class DocumentoViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(150)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El tipo de documento es requerido")]
    [Display(Name = "Tipo de documento")]
    public TipoDocumento Tipo { get; set; } = TipoDocumento.Contrato;

    public ModalidadContrato? Modalidad { get; set; }

    [Display(Name = "Estado")]
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Activo;

    [Display(Name = "URL del archivo")]
    public string UrlArchivo { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Nombre del archivo")]
    public string? NombreArchivo { get; set; }

    [StringLength(100)]
    [Display(Name = "Tipo de contenido")]
    public string? ContentType { get; set; }

    [Display(Name = "Tamaño (bytes)")]
    public long? TamanioBytes { get; set; }

    [Display(Name = "Fecha de expiración")]
    public DateTime? FechaExpiracion { get; set; }

    [StringLength(50)]
    [Display(Name = "Número de folio")]
    public string? NumeroFolio { get; set; }

    [StringLength(255)]
    [Display(Name = "URL externa")]
    public string? UrlExterna { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "El empleado es requerido")]
    [Display(Name = "Empleado")]
    public int EmpleadoId { get; set; }

    public int? DepartamentoId { get; set; }

    public IFormFile? Archivo { get; set; }
}

public class DocumentoListViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Modalidad { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string UrlArchivo { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public string? FechaExpiracion { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;
    public string? EmpleadoDepartamento { get; set; }
    public bool EstaExpirado { get; set; }
    public bool PorExpirar { get; set; }
}

public class DocumentoDetalleViewModel : DocumentoViewModel
{
    public string EmpleadoNombre { get; set; } = string.Empty;
    public string EmpleadoCodigo { get; set; } = string.Empty;
    public string EmpleadoDepartamento { get; set; } = string.Empty;
    public bool EstaExpirado { get; set; }
    public bool PorExpirar { get; set; }
    public string FechaCreacion { get; set; } = string.Empty;
}

public class DocumentoAlertaViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TipoAlerta { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string EmpleadoNombre { get; set; } = string.Empty;
    public int EmpleadoId { get; set; }
    public int DocumentoId { get; set; }
    public string? FechaVencimiento { get; set; }
    public int DiasRestantes { get; set; }
}

public class ExpedienteEmpleadoViewModel
{
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;
    public string EmpleadoCodigo { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public List<DocumentoListViewModel> Documentos { get; set; } = new();
    public List<DocumentoRequeridoViewModel> Requeridos { get; set; } = new();
    public int DocumentosCompletos { get; set; }
    public int DocumentosTotales { get; set; }
    public double PorcentajeCompletado { get; set; }
}

public class DocumentoRequeridoViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EstaPresente { get; set; }
    public int? DocumentoId { get; set; }
    public string? Notes { get; set; }
}