using Microsoft.AspNetCore.Identity;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Seeder;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Seed Roles
        string[] roles = { "SuperAdmin", "RRHH", "Gerente", "Empleado" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed Usuario SuperAdmin
        const string adminEmail = "admin@sice.gt";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador del Sistema",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }

        // Seed Departamentos base
        if (!context.Departamentos.Any())
        {
            var departamentos = new List<Departamento>
            {
                new() { Codigo = "RRHH", Nombre = "Recursos Humanos", Descripcion = "Gestión del personal" },
                new() { Codigo = "CONT", Nombre = "Contabilidad", Descripcion = "Finanzas y contabilidad" },
                new() { Codigo = "OPER", Nombre = "Operaciones", Descripcion = "Operaciones generales" },
                new() { Codigo = "TI",   Nombre = "Tecnología", Descripcion = "Sistemas e infraestructura" },
                new() { Codigo = "VTA",  Nombre = "Ventas", Descripcion = "Fuerza de ventas" },
            };
            context.Departamentos.AddRange(departamentos);
            await context.SaveChangesAsync();

            // Seed Puestos
            var rrhh = context.Departamentos.First(d => d.Codigo == "RRHH");
            var ti = context.Departamentos.First(d => d.Codigo == "TI");

            var puestos = new List<Puesto>
            {
                new() { Codigo = "GG",   Nombre = "Gerente General",    DepartamentoId = rrhh.Id, SalarioMinimo = 15000, SalarioMaximo = 25000, NivelJerarquico = 1 },
                new() { Codigo = "JRRHH",Nombre = "Jefe de RRHH",       DepartamentoId = rrhh.Id, SalarioMinimo = 8000,  SalarioMaximo = 12000, NivelJerarquico = 2 },
                new() { Codigo = "AUX",  Nombre = "Auxiliar de RRHH",   DepartamentoId = rrhh.Id, SalarioMinimo = 3000,  SalarioMaximo = 5000,  NivelJerarquico = 4 },
                new() { Codigo = "DEV",  Nombre = "Desarrollador",       DepartamentoId = ti.Id,   SalarioMinimo = 5000,  SalarioMaximo = 10000, NivelJerarquico = 3 },
                new() { Codigo = "JTI",  Nombre = "Jefe de TI",          DepartamentoId = ti.Id,   SalarioMinimo = 9000,  SalarioMaximo = 14000, NivelJerarquico = 2 },
            };
            context.Puestos.AddRange(puestos);
            await context.SaveChangesAsync();
        }
    }
}