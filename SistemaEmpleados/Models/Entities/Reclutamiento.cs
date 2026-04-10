using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpleados.Models.Entities;

public enum EstadoPlaza { Abierta, EnProceso, Cerrada, Cancelada }
public enum EtapaCandidato { Recibido, Entrevista, Pruebas, Oferta, Contratado, Rechazado }

public class PlazaVacante : BaseEntity
{
    [Required] public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? RequisitoMinimos { get; set; }

    public bool EsReemplazo { get; set; } = false;
    public string? MotivoApertura { get; set; }

    public string? SolicitadoPor { get; set; }
    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }

    public string? FuenteReclutamiento { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalarioOfrecido { get; set; }

    public DateTime FechaPublicacion { get; set; } = DateTime.Today;
    public DateTime? FechaCierre { get; set; }
    public int CantidadVacantes { get; set; } = 1;
    public EstadoPlaza Estado { get; set; } = EstadoPlaza.Abierta;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;

    public int? PuestoId { get; set; }
    public Puesto? Puesto { get; set; }

    public ICollection<Candidato> Candidatos { get; set; } = new List<Candidato>();
    public ICollection<HistorialEstadoPlaza> Historial { get; set; }
        = new List<HistorialEstadoPlaza>();
}

public class Candidato : BaseEntity
{
    [Required] public string Nombre { get; set; } = string.Empty;
    [Required] public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? CvUrl { get; set; }
    public string? FuentePostulacion { get; set; }
    public string? NombreReferido { get; set; }

    [Column(TypeName = "decimal(3,1)")]
    public decimal CalificacionGeneral { get; set; } = 0;

    public int? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public EtapaCandidato Etapa { get; set; } = EtapaCandidato.Recibido;
    public string? Observacion { get; set; }
    public DateTime FechaPostulacion { get; set; } = DateTime.Today;
    public DateTime? FechaEntrevista { get; set; }

    public int PlazaVacanteId { get; set; }
    public PlazaVacante PlazaVacante { get; set; } = null!;

    public ICollection<Entrevista> Entrevistas { get; set; }
        = new List<Entrevista>();
    public ICollection<NotaCandidato> Notas { get; set; }
        = new List<NotaCandidato>();
}