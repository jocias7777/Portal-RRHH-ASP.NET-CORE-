using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH")]
public class ConfiguracionController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView() : View();
    }

    // ══════════════════════════════════════
    // DEPARTAMENTOS
    // ══════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> GetDepartamentos()
    {
        var lista = await _context.Departamentos
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new {
                d.Id,
                d.Codigo,
                d.Nombre,
                d.Descripcion,
                d.Activo,
                totalPuestos = d.Puestos.Count(p => !p.IsDeleted),
                totalEmpleados = d.Empleados.Count(e => !e.IsDeleted)
            })
            .ToListAsync();
        return Json(new { success = true, data = lista });
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartamento(int id)
    {
        var d = await _context.Departamentos.FindAsync(id);
        if (d == null || d.IsDeleted) return Json(new { success = false });
        return Json(new { success = true, data = new { d.Id, d.Codigo, d.Nombre, d.Descripcion, d.Activo } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDepartamento([FromBody] DepartamentoVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return Json(new { success = false, message = "El nombre es requerido." });

        if (vm.Id == 0)
        {
            if (await _context.Departamentos.AnyAsync(d => d.Codigo == vm.Codigo && !d.IsDeleted))
                return Json(new { success = false, message = "El código ya existe." });

            var dep = new Departamento
            {
                Codigo = vm.Codigo.ToUpper().Trim(),
                Nombre = vm.Nombre.Trim(),
                Descripcion = vm.Descripcion?.Trim(),
                Activo = vm.Activo
            };
            _context.Departamentos.Add(dep);
        }
        else
        {
            var dep = await _context.Departamentos.FindAsync(vm.Id);
            if (dep == null || dep.IsDeleted)
                return Json(new { success = false, message = "Departamento no encontrado." });

            dep.Codigo = vm.Codigo.ToUpper().Trim();
            dep.Nombre = vm.Nombre.Trim();
            dep.Descripcion = vm.Descripcion?.Trim();
            dep.Activo = vm.Activo;

            // Si se desactiva el departamento, pasar todos sus empleados a Inactivo
            if (!vm.Activo)
            {
                var empleados = await _context.Empleados
                    .Where(e => e.DepartamentoId == vm.Id && !e.IsDeleted)
                    .ToListAsync();
                foreach (var emp in empleados)
                    emp.Estado = EstadoEmpleado.Inactivo;
            }
            // Si se reactiva → empleados a Activo
            else
            {
                var empleados = await _context.Empleados
                    .Where(e => e.DepartamentoId == vm.Id && !e.IsDeleted)
                    .ToListAsync();
                foreach (var emp in empleados)
                    emp.Estado = EstadoEmpleado.Activo;
            }
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = vm.Id == 0 ? "Departamento creado." : "Departamento actualizado." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartamento(int id)
    {
        var dep = await _context.Departamentos.FindAsync(id);
        if (dep == null) return Json(new { success = false, message = "No encontrado." });

        var tieneEmpleados = await _context.Empleados.AnyAsync(e => e.DepartamentoId == id && !e.IsDeleted);
        if (tieneEmpleados)
            return Json(new { success = false, message = "No se puede eliminar: tiene empleados activos." });

        dep.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Departamento eliminado." });
    }

    // ══════════════════════════════════════
    // PUESTOS
    // ══════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> GetPuestos(int? departamentoId)
    {
        var query = _context.Puestos
            .Include(p => p.Departamento)
            .Where(p => !p.IsDeleted);

        if (departamentoId.HasValue)
            query = query.Where(p => p.DepartamentoId == departamentoId.Value);

        var lista = await query
            .OrderBy(p => p.Departamento.Nombre).ThenBy(p => p.Nombre)
            .Select(p => new {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.SalarioMinimo,
                p.SalarioMaximo,
                p.NivelJerarquico,
                p.Activo,
                p.DepartamentoId,
                departamento = p.Departamento.Nombre,
                totalEmpleados = p.Empleados.Count(e => !e.IsDeleted)
            })
            .ToListAsync();

        return Json(new { success = true, data = lista });
    }

    [HttpGet]
    public async Task<IActionResult> GetPuesto(int id)
    {
        var p = await _context.Puestos.FindAsync(id);
        if (p == null || p.IsDeleted) return Json(new { success = false });
        return Json(new
        {
            success = true,
            data = new
            {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.SalarioMinimo,
                p.SalarioMaximo,
                p.NivelJerarquico,
                p.Activo,
                p.DepartamentoId
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePuesto([FromBody] PuestoVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return Json(new { success = false, message = "El nombre es requerido." });

        if (vm.Id == 0)
        {
            if (await _context.Puestos.AnyAsync(p => p.Codigo == vm.Codigo && !p.IsDeleted))
                return Json(new { success = false, message = "El código ya existe." });

            var puesto = new Puesto
            {
                Codigo = vm.Codigo.ToUpper().Trim(),
                Nombre = vm.Nombre.Trim(),
                Descripcion = vm.Descripcion?.Trim(),
                SalarioMinimo = vm.SalarioMinimo,
                SalarioMaximo = vm.SalarioMaximo,
                NivelJerarquico = vm.NivelJerarquico,
                DepartamentoId = vm.DepartamentoId,
                Activo = vm.Activo
            };
            _context.Puestos.Add(puesto);
        }
        else
        {
            var puesto = await _context.Puestos.FindAsync(vm.Id);
            if (puesto == null || puesto.IsDeleted)
                return Json(new { success = false, message = "Puesto no encontrado." });

            puesto.Codigo = vm.Codigo.ToUpper().Trim();
            puesto.Nombre = vm.Nombre.Trim();
            puesto.Descripcion = vm.Descripcion?.Trim();
            puesto.SalarioMinimo = vm.SalarioMinimo;
            puesto.SalarioMaximo = vm.SalarioMaximo;
            puesto.NivelJerarquico = vm.NivelJerarquico;
            puesto.DepartamentoId = vm.DepartamentoId;
            puesto.Activo = vm.Activo;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = vm.Id == 0 ? "Puesto creado." : "Puesto actualizado." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePuesto(int id)
    {
        var puesto = await _context.Puestos.FindAsync(id);
        if (puesto == null) return Json(new { success = false, message = "No encontrado." });

        var tieneEmpleados = await _context.Empleados.AnyAsync(e => e.PuestoId == id && !e.IsDeleted);
        if (tieneEmpleados)
            return Json(new { success = false, message = "No se puede eliminar: tiene empleados activos." });

        puesto.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Puesto eliminado." });
    }
}

// ── ViewModels locales ──
public class DepartamentoVm
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class PuestoVm
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal SalarioMinimo { get; set; }
    public decimal SalarioMaximo { get; set; }
    public int NivelJerarquico { get; set; } = 1;
    public int DepartamentoId { get; set; }
    public bool Activo { get; set; } = true;
}