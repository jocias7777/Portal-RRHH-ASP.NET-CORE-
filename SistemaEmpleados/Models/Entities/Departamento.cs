namespace SistemaEmpleados.Models.Entities;

public class Departamento : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Puesto> Puestos { get; set; } = new List<Puesto>();
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}