using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class PrestacionesController : Controller
{
    private readonly IPrestacionService _service;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PrestacionesController(
        IPrestacionService service,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView() : View();
    }

    [HttpPost]
    public async Task<IActionResult> GetData([FromBody] DataTablesRequest request)
    {
        var result = await _service.GetDataTablesAsync(request);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Form(int? id)
    {
        var vm = id.HasValue
            ? await _service.GetByIdAsync(id.Value)
            : new PrestacionViewModel { Periodo = DateTime.Now.Year };

        if (vm == null) return NotFound();
        await CargarSelectLists();
        return PartialView("_Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Create([FromBody] PrestacionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            return Json(ApiResponse.Fail("Datos inválidos.", errors));
        }

        var user = await _userManager.GetUserAsync(User);
        var (success, message, newId) = await _service.CreateAsync(vm, user?.NombreCompleto ?? "");
        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Edit(int id, [FromBody] PrestacionViewModel vm)
    {
        var (success, message) = await _service.UpdateAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> MarcarPagado(int id, [FromBody] DateTime fechaPago)
    {
        var (success, message) = await _service.MarcarPagadoAsync(id, fechaPago);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpGet]
    public async Task<IActionResult> Calcular(int empleadoId, int anio = 0)
    {
        if (anio == 0) anio = DateTime.Now.Year;
        var calculo = await _service.CalcularPrestacionesAsync(empleadoId, anio);
        if (calculo == null) return Json(ApiResponse.Fail("Empleado no encontrado."));
        return Json(ApiResponse<object>.Ok(calculo));
    }

    // ── KPIs ──
    [HttpGet]
    public async Task<IActionResult> GetKPIs()
    {
        var anio = DateTime.Now.Year;
        var prestaciones = await _context.Prestaciones
            .Where(p => !p.IsDeleted && p.Periodo == anio)
            .ToListAsync();

        var kpis = new
        {
            totalRegistros = prestaciones.Count,
            pendientes = prestaciones.Count(p => p.Estado != EstadoPrestacion.Pagado),
            totalPagado = prestaciones
                .Where(p => p.Estado == EstadoPrestacion.Pagado)
                .Sum(p => p.Monto),
            montoPendiente = prestaciones
                .Where(p => p.Estado != EstadoPrestacion.Pagado)
                .Sum(p => p.Monto)
        };

        return Json(ApiResponse<object>.Ok(kpis));
    }

    // ── Finiquitos ──
    [HttpGet]
    public async Task<IActionResult> GetFiniquitos()
    {
        var empleados = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .ToListAsync();

        var data = empleados.Select(e => {
            var hoy = DateTime.Today;
            var meses = ((hoy.Year - e.FechaIngreso.Year) * 12) + hoy.Month - e.FechaIngreso.Month;
            var anios = meses / 12;
            var aguinaldo = Math.Round((e.SalarioBase / 12) * Math.Min(meses, 12), 2);
            var bono14 = Math.Round((e.SalarioBase / 12) * Math.Min(meses, 12), 2);
            var indemnizacion = Math.Round(e.SalarioBase * anios, 2);

            return new
            {
                EmpleadoId = e.Id,
                NombreEmpleado = $"{e.PrimerNombre} {e.PrimerApellido}".Trim(),
                Departamento = e.Departamento?.Nombre ?? "—",
                AniosTrabajados = anios,
                SalarioBase = e.SalarioBase,
                Aguinaldo = aguinaldo,
                Bono14 = bono14,
                Indemnizacion = indemnizacion,
                TotalFiniquito = aguinaldo + bono14 + indemnizacion
            };
        }).ToList();

        return Json(new { success = true, data });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetalle(int id)
    {
        var prestacion = await _context.Prestaciones
            .Include(p => p.Empleado)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (prestacion == null) return Json(ApiResponse.Fail("No encontrado."));

        var anio = prestacion.Periodo;
        TipoPrestacion tipo = prestacion.Tipo;

        // Período según tipo
        DateTime inicioPeriodo, finPeriodo;
        if (tipo == TipoPrestacion.Aguinaldo)
        {
            inicioPeriodo = new DateTime(anio - 1, 12, 1);
            finPeriodo = new DateTime(anio, 11, 30);
        }
        else
        {
            inicioPeriodo = new DateTime(anio - 1, 7, 1);
            finPeriodo = new DateTime(anio, 6, 30);
        }

        // Ajustar por fecha de ingreso y salida
        if (prestacion.Empleado.FechaIngreso > inicioPeriodo)
            inicioPeriodo = prestacion.Empleado.FechaIngreso;

        var hoy = DateTime.Today;
        var fechaSalida = prestacion.Empleado.FechaSalida.HasValue
            ? prestacion.Empleado.FechaSalida.Value : hoy;
        if (fechaSalida < finPeriodo) finPeriodo = fechaSalida;

        var diasTotalesPeriodo = Math.Max(0, (finPeriodo - inicioPeriodo).Days + 1);

        // Vacaciones en el período
        var vacaciones = await _context.Vacaciones
            .Where(v => !v.IsDeleted
                     && v.EmpleadoId == prestacion.EmpleadoId
                     && v.Estado == EstadoVacacion.Aprobado
                     && v.FechaInicio >= inicioPeriodo
                     && v.FechaInicio <= finPeriodo)
            .Select(v => new {
                FechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),
                FechaFin = v.FechaFin.ToString("dd/MM/yyyy"),
                DiasHabiles = Math.Max(0, EF.Functions.DateDiffDay(v.FechaInicio, v.FechaFin) + 1),
                v.Observacion
            })
            .ToListAsync();

        // Ausencias injustificadas en el período
        var ausencias = await _context.Ausencias
            .Where(a => !a.IsDeleted
                     && a.EmpleadoId == prestacion.EmpleadoId
                     && !a.Justificada
                     && a.FechaInicio >= inicioPeriodo
                     && a.FechaInicio <= finPeriodo)
            .Select(a => new {
                FechaInicio = a.FechaInicio.ToString("dd/MM/yyyy"),
                FechaFin = a.FechaFin.ToString("dd/MM/yyyy"),
                a.TotalDias,
                Tipo = a.Tipo.ToString()
            })
            .ToListAsync();

        var totalVac = vacaciones.Sum(v => v.DiasHabiles);
        var totalAus = ausencias.Sum(a => a.TotalDias);
        var diasEfectivos = Math.Max(0, diasTotalesPeriodo - totalVac - totalAus);

        var detalle = new
        {
            nombreEmpleado = $"{prestacion.Empleado.PrimerNombre} {prestacion.Empleado.PrimerApellido}",
            tipo = prestacion.Tipo.ToString(),
            periodo = prestacion.Periodo,
            inicioPeriodo = inicioPeriodo.ToString("dd/MM/yyyy"),
            finPeriodo = finPeriodo.ToString("dd/MM/yyyy"),
            diasTotalesPeriodo,
            totalVacaciones = totalVac,
            totalAusencias = totalAus,
            diasEfectivos,
            salarioBase = prestacion.SalarioBase,
            monto = prestacion.Monto,
            estado = prestacion.Estado.ToString(),
            vacaciones,
            ausencias
        };

        return Json(ApiResponse<object>.Ok(detalle));
    }

    // ── Marcar todas como pagadas ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> MarcarTodasPagadas([FromBody] MarcarTodasViewModel vm)
    {
        var query = _context.Prestaciones
            .Where(p => !p.IsDeleted && p.Estado != EstadoPrestacion.Pagado);

        if (!string.IsNullOrEmpty(vm.Tipo) &&
            Enum.TryParse<TipoPrestacion>(vm.Tipo, out var tipo))
            query = query.Where(p => p.Tipo == tipo);

        if (vm.Periodo.HasValue)
            query = query.Where(p => p.Periodo == vm.Periodo);

        var lista = await query.ToListAsync();
        foreach (var p in lista)
        {
            p.Estado = EstadoPrestacion.Pagado;
            p.FechaPago = vm.FechaPago;
            p.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Json(ApiResponse.Ok($"Se marcaron {lista.Count} prestaciones como pagadas."));
    }

    // ── Generar PDF ──
    [HttpGet]
    public async Task<IActionResult> GenerarPDF(string? tipo, string? estado, int? periodo)
    {
        var query = _context.Prestaciones
            .Include(p => p.Empleado).ThenInclude(e => e.Departamento)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(tipo) &&
            Enum.TryParse<TipoPrestacion>(tipo, out var tipoPrest))
            query = query.Where(p => p.Tipo == tipoPrest);

        if (!string.IsNullOrEmpty(estado) &&
            Enum.TryParse<EstadoPrestacion>(estado, out var est))
            query = query.Where(p => p.Estado == est);

        if (periodo.HasValue)
            query = query.Where(p => p.Periodo == periodo);

        var datos = await query
            .OrderBy(p => p.Empleado.PrimerApellido)
            .ThenBy(p => p.Tipo)
            .ToListAsync();

        // Generar HTML para el PDF
        var html = GenerarHTMLPDF(datos, tipo, periodo);
        return Content(html, "text/html");
    }

    private string GenerarHTMLPDF(
        List<Prestacion> datos, string? tipo, int? periodo)
    {
        var filas = datos.Select(p => $@"
        <tr>
            <td>{p.Empleado.PrimerNombre} {p.Empleado.PrimerApellido}</td>
            <td>{p.Empleado.Departamento?.Nombre}</td>
            <td>{p.Tipo}</td>
            <td>{p.Periodo}</td>
            <td>{p.MesesTrabajados}</td>
            <td>Q {p.SalarioBase:N2}</td>
            <td><strong>Q {p.Monto:N2}</strong></td>
            <td>{p.Estado}</td>
            <td>{p.FechaPago?.ToString("dd/MM/yyyy") ?? "—"}</td>
        </tr>").Aggregate("", (a, b) => a + b);

        var totalMonto = datos.Sum(p => p.Monto);

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<title>Prestaciones {tipo} {periodo}</title>
<style>
    body {{ font-family: Arial, sans-serif; font-size: 12px; margin: 20px; }}
    h2 {{ color: #1E293B; margin-bottom: 4px; }}
    p {{ color: #64748B; margin: 0 0 16px; }}
    table {{ width: 100%; border-collapse: collapse; }}
    th {{ background: #F8FAFC; padding: 8px; text-align: left;
          border-bottom: 2px solid #E2E8F0; font-size: 11px;
          text-transform: uppercase; color: #64748B; }}
    td {{ padding: 7px 8px; border-bottom: 1px solid #F1F5F9; }}
    tr:hover td {{ background: #F8FAFC; }}
    .total {{ background: #F0FDF4; font-weight: bold; }}
    .total td {{ border-top: 2px solid #BBF7D0; color: #065F46; }}
    @media print {{ body {{ margin: 10px; }} }}
</style>
</head>
<body>
<h2>SICE — Prestaciones Laborales</h2>
<p>Tipo: {tipo ?? "Todos"} | Período: {periodo?.ToString() ?? "Todos"} |
   Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Total registros: {datos.Count}</p>
<table>
<thead>
<tr>
    <th>Empleado</th><th>Departamento</th><th>Tipo</th>
    <th>Período</th><th>Meses</th><th>Salario</th>
    <th>Monto</th><th>Estado</th><th>Fecha pago</th>
</tr>
</thead>
<tbody>
{filas}
<tr class='total'>
    <td colspan='6'><strong>TOTAL</strong></td>
    <td><strong>Q {totalMonto:N2}</strong></td>
    <td colspan='2'>{datos.Count} registros</td>
</tr>
</tbody>
</table>
<script>window.print();</script>
</body>
</html>";
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> GenerarAnio([FromBody] GenerarPrestacionesAnioViewModel vm)
    {
        if (vm == null || vm.Anio < 2000 || vm.Anio > 2099)
            return Json(ApiResponse.Fail("Año inválido."));

        var user = await _userManager.GetUserAsync(User);
        var (success, message, cantidad) =
            await _service.GenerarPrestacionesAnioAsync(
                vm.Anio,
                user?.NombreCompleto ?? "",
                vm.DepartamentoId);
        return Json(success
            ? ApiResponse<object>.Ok(new { cantidad }, message)
            : ApiResponse.Fail(message));
    }

    private async Task CargarSelectLists()
    {
        ViewBag.Empleados = await _context.Empleados
            .Where(e => !e.IsDeleted && e.Estado == EstadoEmpleado.Activo)
            .OrderBy(e => e.PrimerApellido)
            .Select(e => new { e.Id, Nombre = $"{e.PrimerNombre} {e.PrimerApellido}" })
            .ToListAsync();
    }
}
