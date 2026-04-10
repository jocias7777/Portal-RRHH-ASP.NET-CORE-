namespace SistemaEmpleados.Models.DTOs;

/// <summary>
/// Parámetros estándar enviados por DataTables server-side
/// </summary>
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? SearchValue { get; set; }
    public string? OrderColumn { get; set; }
    public string? OrderDir { get; set; }

    // Filtros adicionales opcionales
    public int? DepartamentoId { get; set; }
    public string? Estado { get; set; }
    public string? FechaDesde { get; set; }
    public string? FechaHasta { get; set; }
    public string? TipoPrestacion { get; set; }
    public int? EmpleadoId { get; set; }
    public string? Tipo { get; set; }
}

public class DataTablesResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }
}