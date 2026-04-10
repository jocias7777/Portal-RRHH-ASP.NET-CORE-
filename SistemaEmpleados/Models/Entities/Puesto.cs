namespace SistemaEmpleados.Models.Entities;

public class Puesto : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal SalarioMinimo { get; set; }
    public decimal SalarioMaximo { get; set; }
    public int NivelJerarquico { get; set; } = 1;
    public bool Activo { get; set; } = true;

    public int DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = null!;
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}