namespace SistemaEmpleados.Models.ViewModels;

public class PlanillaListViewModel
{
    public int Id { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public int Mes { get; set; }
    public int Anio { get; set; }
    public int TotalEmpleados { get; set; }
    public decimal TotalDevengado { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal TotalNeto { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string GeneradoPor { get; set; } = string.Empty;
    public string FechaGeneracion { get; set; } = string.Empty;
    public string? FechaPago { get; set; }
}

public class DetallePlanillaViewModel
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public decimal SalarioBase { get; set; }
    public decimal HorasExtraMonto { get; set; }
    public decimal Bonificacion250 { get; set; }
    public decimal OtrosBonos { get; set; }
    public decimal TotalDevengado { get; set; }
    public decimal CuotaIGSS { get; set; }
    public decimal ISR { get; set; }
    public decimal OtrasDeducciones { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal SalarioNeto { get; set; }
    public string? Observacion { get; set; }
    public decimal CuotaIGSSPatronal { get; set; }
    public decimal Bono14 { get; set; }
    public decimal Aguinaldo { get; set; }
    public decimal DescuentoPrestamo { get; set; }
}

public class DetallePlanillaEditViewModel
{
    public decimal OtrosBonos { get; set; }
    public decimal OtrasDeducciones { get; set; }
    public string? Observacion { get; set; }
}

public class PlanillaViewModel
{
    public int Mes { get; set; }
    public int Anio { get; set; }
}

public class BoletaPagoViewModel
{
    public int EmpleadoId { get; set; }
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string NumeroIGSS { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string FechaPago { get; set; } = string.Empty;
    public decimal SalarioBase { get; set; }
    public decimal HorasExtraMonto { get; set; }
    public decimal Bonificacion250 { get; set; }
    public decimal OtrosBonos { get; set; }
    public decimal TotalDevengado { get; set; }
    public decimal CuotaIGSS { get; set; }
    public decimal ISR { get; set; }
    public decimal OtrasDeducciones { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal SalarioNeto { get; set; }
    public string? Observacion { get; set; }
}

public class ResumenNominaViewModel
{
    public int Anio { get; set; }
    public int TotalPlanillas { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal TotalDevengadoAnio { get; set; }
    public decimal TotalDeduccionesAnio { get; set; }
    public List<ResumenMesViewModel> PlanillasPorMes { get; set; } = new();
}

public class ActualizarSalarioViewModel
{
    public decimal SalarioBase { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}

public class PrestamoViewModel
{
    public int EmpleadoId { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal CuotaMensual { get; set; }
    public int NumeroCuotas { get; set; }
    public string? Motivo { get; set; }
    public string FechaInicio { get; set; } = string.Empty;
}

public class PrestamoListViewModel
{
    public int Id { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public decimal CuotaMensual { get; set; }
    public int NumeroCuotas { get; set; }
    public int CuotasPagadas { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

public class ConceptoNominaViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Aplicacion { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public bool EsObligatorio { get; set; }
    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;
}

public class ConceptoListViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Aplicacion { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public bool EsObligatorio { get; set; }
    public bool EsSistema { get; set; }
    public bool Activo { get; set; }
}

public class AbonoViewModel
{
    public decimal Monto { get; set; }
}
public class ResumenMesViewModel
{
    public string Mes { get; set; } = string.Empty;
    public decimal TotalNeto { get; set; }
    public string Estado { get; set; } = string.Empty;
}