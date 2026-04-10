using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;

namespace SistemaEmpleados.Controllers;

[Authorize]
public class ApiController : Controller
{
    private readonly ApplicationDbContext _context;

    public ApiController(ApplicationDbContext ctx) => _context = ctx;

    [HttpGet("/api/departamentos")]
    public async Task<IActionResult> GetDepartamentos()
    {
        var data = await _context.Departamentos
            .Where(d => d.Activo && !d.IsDeleted)
            .OrderBy(d => d.Nombre)
            .Select(d => new { d.Id, d.Nombre })
            .ToListAsync();

        return Json(ApiResponse<object>.Ok(data));
    }

    [HttpGet("/api/dashboard/kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var total = await _context.Empleados.CountAsync(e => !e.IsDeleted);
        var activos = await _context.Empleados.CountAsync(e => !e.IsDeleted &&
            e.Estado == SistemaEmpleados.Models.Entities.EstadoEmpleado.Activo);

        return Json(ApiResponse<object>.Ok(new
        {
            totalEmpleados = total,
            activos,
            incapacidades = 0,
            costoNomina = 0m
        }));
    }

    [HttpGet("/api/search")]
    public IActionResult Search(string q)
    {
        return Json(new { groups = Array.Empty<object>() });
    }
}