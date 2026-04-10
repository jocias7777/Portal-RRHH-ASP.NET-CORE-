using System.ComponentModel.DataAnnotations;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

public class EmpleadoViewModel
{
    public int Id { get; set; }

    // ── Datos personales
    [Required(ErrorMessage = "El primer nombre es requerido")]
    [StringLength(50)]
    [Display(Name = "Primer nombre")]
    public string PrimerNombre { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Segundo nombre")]
    public string? SegundoNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es requerido")]
    [StringLength(50)]
    [Display(Name = "Primer apellido")]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Segundo apellido")]
    public string? SegundoApellido { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime FechaNacimiento { get; set; }

    [Display(Name = "Género")]
    public Genero Genero { get; set; }

    // ── Documentos
    [Required(ErrorMessage = "El CUI/DPI es requerido")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "El CUI debe tener 13 dígitos")]
    [Display(Name = "CUI / DPI")]
    public string CUI { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "NIT")]
    public string? NIT { get; set; }

    [StringLength(20)]
    [Display(Name = "No. IGSS")]
    public string? NumeroIGSS { get; set; }

    [StringLength(20)]
    [Display(Name = "No. IRTRA")]
    public string? NumeroIRTRA { get; set; }

    // ── Contacto
    [StringLength(200)]
    [Display(Name = "Foto URL")]
    public string? FotoUrl { get; set; }

    [Phone]
    [StringLength(15)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    [StringLength(100)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    // ── Laboral
    [Required(ErrorMessage = "La fecha de ingreso es requerida")]
    [Display(Name = "Fecha de ingreso")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    [Display(Name = "Fecha de salida")]
    public DateTime? FechaSalida { get; set; }

    [Display(Name = "Estado")]
    public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.Activo;

    [Display(Name = "Tipo de contrato")]
    public TipoContrato TipoContrato { get; set; }

    [Required(ErrorMessage = "El salario base es requerido")]
    [Range(100, 999999, ErrorMessage = "Salario debe estar entre Q100 y Q999,999")]
    [Display(Name = "Salario base (Q)")]
    public decimal SalarioBase { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // ── FKs
    [Required(ErrorMessage = "El departamento es requerido")]
    [Display(Name = "Departamento")]
    public int DepartamentoId { get; set; }

    [Required(ErrorMessage = "El puesto es requerido")]
    [Display(Name = "Puesto")]
    public int PuestoId { get; set; }
}

public class EmpleadoListViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public string TipoContrato { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FechaIngreso { get; set; } = string.Empty;
    public decimal SalarioBase { get; set; }
}

public class EmpleadoDetalleViewModel : EmpleadoViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreDepartamento { get; set; } = string.Empty;
    public string NombrePuesto { get; set; } = string.Empty;
    public int AniosServicio { get; set; }
}