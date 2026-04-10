using System.ComponentModel.DataAnnotations;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Models.ViewModels;

// ── Lista de plazas para DataTable ──
public class PlazaVacanteListViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public decimal SalarioOfrecido { get; set; }
    public int CantidadVacantes { get; set; }
    public int TotalCandidatos { get; set; }
    public int CandidatosActivos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FechaPublicacion { get; set; } = string.Empty;
    public string? FechaCierre { get; set; }
    public bool EsReemplazo { get; set; }
    public string? FuenteReclutamiento { get; set; }
    public int DiasAbierta { get; set; }
}

// ── Formulario de plaza ──
public class PlazaVacanteViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es requerido")]
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public string? RequisitoMinimos { get; set; }
    public decimal SalarioOfrecido { get; set; }
    public DateTime FechaPublicacion { get; set; } = DateTime.Today;
    public DateTime? FechaCierre { get; set; }
    public int CantidadVacantes { get; set; } = 1;
    public EstadoPlaza Estado { get; set; } = EstadoPlaza.Abierta;

    [Required(ErrorMessage = "El departamento es requerido")]
    public int DepartamentoId { get; set; }
    public int? PuestoId { get; set; }

    public bool EsReemplazo { get; set; } = false;
    public string? MotivoApertura { get; set; }
    public string? FuenteReclutamiento { get; set; }
}

// ── Lista de candidatos para DataTable ──
public class CandidatoListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Plaza { get; set; } = string.Empty;
    public int PlazaId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;
    public string FechaPostulacion { get; set; } = string.Empty;
    public string? FechaEntrevista { get; set; }
    public string? CvUrl { get; set; }
    public string? FuentePostulacion { get; set; }
    public decimal CalificacionGeneral { get; set; }
    public int TotalEntrevistas { get; set; }
    public int TotalNotas { get; set; }
    public bool FueContratado { get; set; }
}

// ── Formulario de candidato ──
public class CandidatoViewModel
{
    public int Id { get; set; }

    [Required]
    public int PlazaVacanteId { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Telefono { get; set; }
    public string? CvUrl { get; set; }
    public EtapaCandidato Etapa { get; set; } = EtapaCandidato.Recibido;
    public string? Observacion { get; set; }
    public DateTime? FechaEntrevista { get; set; }
    public string? FuentePostulacion { get; set; }
    public string? NombreReferido { get; set; }
}

// ── Detalle completo de plaza (para modal) ──
public class PlazaDetalleViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public string? Descripcion { get; set; }
    public string? RequisitoMinimos { get; set; }
    public decimal SalarioOfrecido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FechaPublicacion { get; set; } = string.Empty;
    public string? FechaCierre { get; set; }
    public int CantidadVacantes { get; set; }
    public bool EsReemplazo { get; set; }
    public string? MotivoApertura { get; set; }
    public string? FuenteReclutamiento { get; set; }
    public int DiasAbierta { get; set; }

    // Pipeline de candidatos por etapa
    public int TotalCandidatos { get; set; }
    public int Recibidos { get; set; }
    public int EnEntrevista { get; set; }
    public int EnPruebas { get; set; }
    public int EnOferta { get; set; }
    public int Contratados { get; set; }
    public int Rechazados { get; set; }

    public List<CandidatoListViewModel> Candidatos { get; set; } = new();
    public List<HistorialEstadoViewModel> Historial { get; set; } = new();
}

// ── Historial de estado de plaza ──
public class HistorialEstadoViewModel
{
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string CambiadoPor { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
}

// ── Entrevista ──
public class EntrevistaViewModel
{
    public int Id { get; set; }
    public int CandidatoId { get; set; }
    public string NombreCandidato { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public string Entrevistador { get; set; } = string.Empty;
    public string? Lugar { get; set; }
    public string Resultado { get; set; } = "Pendiente";
    public decimal Calificacion { get; set; } = 0;
    public string? Observaciones { get; set; }
}

// ── Nota de candidato ──
public class NotaCandidatoViewModel
{
    public int Id { get; set; }
    public int CandidatoId { get; set; }
    public string Nota { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
}

// ── Estadísticas del módulo ──
public class EstadisticasReclutamientoViewModel
{
    public int PlazasAbiertas { get; set; }
    public int PlazasEnProceso { get; set; }
    public int PlazasCerradas { get; set; }
    public int TotalCandidatos { get; set; }
    public int CandidatosEsteMes { get; set; }
    public int Contratados { get; set; }
    public double TiempoPromedioContratacion { get; set; }
    public double TasaConversion { get; set; }
}

// ── Cambiar estado de plaza ──
public class CambiarEstadoPlazaViewModel
{
    public string Estado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

// ── Convertir candidato en empleado ──
public class ConvertirEmpleadoViewModel
{
    public int CandidatoId { get; set; }
    public decimal SalarioBase { get; set; }
    public int DepartamentoId { get; set; }
    public int PuestoId { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Today;
    public string TipoContrato { get; set; } = "Indefinido";
}

// ── Registrar oferta al candidato ──
public class OfertaCandidatoViewModel
{
    public int CandidatoId { get; set; }
    public decimal SalarioOferta { get; set; }
    public DateTime FechaIngresoPropuesta { get; set; } = DateTime.Today;
    public string TipoContrato { get; set; } = "Indefinido";
    public string? Observaciones { get; set; }
}
