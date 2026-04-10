using Microsoft.AspNetCore.Identity;

namespace SistemaEmpleados.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Relación con Empleado (fase 2)
    public int? EmpleadoId { get; set; }
}