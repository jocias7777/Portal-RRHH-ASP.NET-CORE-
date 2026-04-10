namespace SistemaEmpleados.Models.ViewModels;

public class AsistenciaKpiViewModel
{
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public int Tardanzas { get; set; }
    public decimal HorasExtra { get; set; }
    public int TotalEmpleados { get; set; }
}

public class HorarioListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string HoraEntrada { get; set; } = string.Empty;
    public string HoraSalida { get; set; } = string.Empty;
    public int MinutosToleranciaTardanza { get; set; }
    public bool Activo { get; set; }
    public int TotalEmpleados { get; set; } // cuántos empleados usan este horario
}