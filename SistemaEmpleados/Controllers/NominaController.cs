using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services;
using SistemaEmpleados.Services.Interfaces;


namespace SistemaEmpleados.Controllers;

[Authorize(Roles = "SuperAdmin,RRHH,Gerente")]
public class NominaController : Controller
{
    private readonly INominaService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public NominaController(
        INominaService service,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
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
    public async Task<IActionResult> Detalle(int id)
    {
        var planilla = await _service.GetByIdAsync(id);
        if (planilla == null) return NotFound();

        var detalles = await _service.GetDetallesAsync(id);
        ViewBag.Planilla = planilla;
        ViewBag.Detalles = detalles;

        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView("_Detalle") : View("Detalle");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Generar([FromBody] PlanillaViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        var (success, message, newId) =
            await _service.GenerarPlanillaAsync(
                vm.Mes, vm.Anio, user?.NombreCompleto ?? User.Identity?.Name ?? "Sistema");

        return Json(success
            ? ApiResponse<object>.Ok(new { id = newId }, message)
            : ApiResponse<object>.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> MarcarPagada(int id, [FromBody] DateTime fechaPago)
    {
        var (success, message) = await _service.MarcarPagadaAsync(id, fechaPago);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Anular(int id)
    {
        var (success, message) = await _service.AnularAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> ActualizarDetalle(
        int id, [FromBody] DetallePlanillaEditViewModel vm)
    {
        var (success, message) = await _service.ActualizarDetalleAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── Boleta de pago individual ──
    [HttpGet]
    public async Task<IActionResult> Boleta(int id)
    {
        var boleta = await _service.GetBoletaPagoAsync(id);
        if (boleta == null) return NotFound();

        var isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        return isPartial ? PartialView("_Boleta", boleta) : View("Boleta", boleta);
    }

    // ── Resumen anual ──
    [HttpGet]
    public async Task<IActionResult> ResumenAnio(int anio = 0)
    {
        if (anio == 0) anio = DateTime.Now.Year;
        var vm = await _service.GetResumenAnioAsync(anio);
        return Json(vm);
    }

    // ── Historial de un empleado ──
    [HttpGet]
    public async Task<IActionResult> HistorialEmpleado(int empleadoId)
    {
        var historial = await _service.GetHistorialEmpleadoAsync(empleadoId);
        return Json(historial);
    }

    // ── Salarios ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> ActualizarSalario(
        int id, [FromBody] ActualizarSalarioViewModel vm)
    {
        var (success, message) = await _service.ActualizarSalarioAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── Préstamos ──
    [HttpGet]
    public async Task<IActionResult> GetPrestamos()
    {
        var data = await _service.GetPrestamosAsync();
        return Json(new { success = true, data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CrearPrestamo(
        [FromBody] PrestamoViewModel vm)
    {
        var (success, message) = await _service.CrearPrestamoAsync(vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CancelarPrestamo(int id)
    {
        var (success, message) = await _service.CancelarPrestamoAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── Conceptos ──
    [HttpGet]
    public async Task<IActionResult> GetConceptos()
    {
        var data = await _service.GetConceptosAsync();
        return Json(new { success = true, data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> CrearConcepto(
        [FromBody] ConceptoNominaViewModel vm)
    {
        var (success, message) = await _service.CrearConceptoAsync(vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EditarConcepto(
        int id, [FromBody] ConceptoNominaViewModel vm)
    {
        var (success, message) = await _service.EditarConceptoAsync(id, vm);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EliminarPrestamo(int id)
    {
        var (success, message) = await _service.EliminarPrestamoAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    // ── Abonar cuota ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> AbonarCuota(
        int id, [FromBody] AbonoViewModel vm)
    {
        var (success, message) = await _service.AbonarCuotaAsync(id, vm.Monto);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (success, message) = await _service.EliminarPlanillaAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }


    // ── HTML Boleta ──
    private static string RenderBoletaHtml(
        SistemaEmpleados.Models.ViewModels.BoletaPagoViewModel b)
    {
        var cult = new System.Globalization.CultureInfo("es-GT");
        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #0f172a; padding: 20px; }}
  .header {{ background: #0f172a; color: #fff; padding: 20px 24px; border-radius: 8px 8px 0 0; display: flex; justify-content: space-between; align-items: center; border-bottom: 3px solid #f59e0b; }}
  .header h1 {{ font-size: 18px; font-weight: 700; }}
  .header .periodo {{ font-size: 13px; color: #94a3b8; margin-top: 4px; }}
  .header .logo {{ font-size: 24px; color: #f59e0b; }}
  .emp-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 0; padding: 16px 24px; display: flex; gap: 40px; flex-wrap: wrap; }}
  .emp-field label {{ font-size: 10px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: .6px; display: block; margin-bottom: 3px; }}
  .emp-field span {{ font-size: 13px; color: #0f172a; font-weight: 600; }}
  .section {{ margin: 0; }}
  .section-title {{ background: #1e293b; color: #fff; padding: 8px 24px; font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; }}
  table {{ width: 100%; border-collapse: collapse; }}
  table td {{ padding: 9px 24px; border-bottom: 1px solid #f1f5f9; font-size: 12px; }}
  table td:last-child {{ text-align: right; font-weight: 600; font-family: 'Consolas', monospace; }}
  .total-row td {{ background: #0f172a; color: #fff; font-weight: 700; font-size: 13px; padding: 12px 24px; }}
  .total-row td:last-child {{ color: #86efac; font-size: 15px; }}
  .ded-row td:last-child {{ color: #dc2626; }}
  .footer-box {{ background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 0 0 8px 8px; padding: 16px 24px; display: flex; justify-content: space-between; align-items: center; }}
  .footer-box .neto-label {{ font-size: 11px; color: #166534; font-weight: 700; text-transform: uppercase; letter-spacing: .6px; }}
  .footer-box .neto-value {{ font-size: 28px; font-weight: 700; color: #16a34a; font-family: 'Consolas', monospace; }}
  .footer-legal {{ margin-top: 24px; font-size: 10px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; padding-top: 12px; }}
  .firma-box {{ margin-top: 40px; display: flex; justify-content: space-around; }}
  .firma {{ text-align: center; }}
  .firma-line {{ border-top: 1px solid #0f172a; width: 180px; margin: 0 auto 6px; }}
  .firma-label {{ font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: .5px; }}
</style>
</head>
<body>
  <div class='header'>
    <div>
      <h1>Boleta de Pago</h1>
      <div class='periodo'>{b.Periodo} &nbsp;·&nbsp; Fecha de pago: {b.FechaPago}</div>
    </div>
    <div class='logo'>&#xe533;</div>
  </div>

  <div class='emp-card'>
    <div class='emp-field'>
      <label>Empleado</label>
      <span>{b.NombreEmpleado}</span>
    </div>
    <div class='emp-field'>
      <label>Código</label>
      <span>{b.CodigoEmpleado}</span>
    </div>
    <div class='emp-field'>
      <label>Departamento</label>
      <span>{b.Departamento}</span>
    </div>
    <div class='emp-field'>
      <label>Puesto</label>
      <span>{b.Puesto}</span>
    </div>
    <div class='emp-field'>
      <label>NIT</label>
      <span>{b.NIT}</span>
    </div>
    <div class='emp-field'>
      <label>No. IGSS</label>
      <span>{b.NumeroIGSS}</span>
    </div>
  </div>

  <div class='section'>
    <div class='section-title'>Ingresos</div>
    <table>
      <tr><td>Salario base</td><td>Q {b.SalarioBase.ToString("N2", cult)}</td></tr>
      {(b.HorasExtraMonto > 0 ? $"<tr><td>Horas extra</td><td>Q {b.HorasExtraMonto.ToString("N2", cult)}</td></tr>" : "")}
      <tr><td>Bonificación incentivo (Dto. 78-89)</td><td>Q {b.Bonificacion250.ToString("N2", cult)}</td></tr>
      {(b.OtrosBonos > 0 ? $"<tr><td>Otros bonos</td><td>Q {b.OtrosBonos.ToString("N2", cult)}</td></tr>" : "")}
      <tr style='background:#f8fafc'><td><strong>Total devengado</strong></td><td><strong>Q {b.TotalDevengado.ToString("N2", cult)}</strong></td></tr>
    </table>
  </div>

  <div class='section'>
    <div class='section-title'>Deducciones</div>
    <table>
      <tr class='ded-row'><td>IGSS laboral (4.83%)</td><td>Q {b.CuotaIGSS.ToString("N2", cult)}</td></tr>
      {(b.ISR > 0 ? $"<tr class='ded-row'><td>ISR (Impuesto Sobre la Renta)</td><td>Q {b.ISR.ToString("N2", cult)}</td></tr>" : "")}
      {(b.OtrasDeducciones > 0 ? $"<tr class='ded-row'><td>Otras deducciones</td><td>Q {b.OtrasDeducciones.ToString("N2", cult)}</td></tr>" : "")}
      <tr style='background:#fef2f2'><td><strong>Total deducciones</strong></td><td style='color:#dc2626'><strong>Q {b.TotalDeducciones.ToString("N2", cult)}</strong></td></tr>
    </table>
  </div>

  <div class='footer-box'>
    <div>
      <div class='neto-label'>Salario neto a recibir</div>
      {(!string.IsNullOrEmpty(b.Observacion) ? $"<div style='font-size:10px;color:#92400e;margin-top:4px'>{b.Observacion}</div>" : "")}
    </div>
    <div class='neto-value'>Q {b.SalarioNeto.ToString("N2", cult)}</div>
  </div>

  <div class='firma-box'>
    <div class='firma'>
      <div class='firma-line'></div>
      <div class='firma-label'>Firma Empleado</div>
    </div>
    <div class='firma'>
      <div class='firma-line'></div>
      <div class='firma-label'>Recursos Humanos</div>
    </div>
    <div class='firma'>
      <div class='firma-line'></div>
      <div class='firma-label'>Gerencia</div>
    </div>
  </div>

  <div class='footer-legal'>
    Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} &nbsp;·&nbsp; 
    Este documento es confidencial y de uso interno.
  </div>
</body>
</html>";
    }

    // ── HTML Planilla completa ──
    private static string RenderPlanillaHtml(
        SistemaEmpleados.Models.ViewModels.PlanillaListViewModel p,
        IEnumerable<SistemaEmpleados.Models.ViewModels.DetallePlanillaViewModel> detalles)
    {
        var cult = new System.Globalization.CultureInfo("es-GT");
        var lista = detalles.ToList();

        decimal totalPatronal = lista.Sum(d => d.CuotaIGSSPatronal);
        decimal totalSalarios = lista.Sum(d => d.SalarioBase);
        decimal costoEmpresa = p.TotalDevengado + totalPatronal + (totalSalarios * 0.02m);

        var filas = string.Join("", lista.Select((d, i) => $@"
      <tr style='background:{(i % 2 == 0 ? "#fff" : "#f8fafc")}'>
        <td>{i + 1}</td>
        <td>
          <div style='font-weight:600'>{d.NombreEmpleado}</div>
          <div style='font-size:10px;color:#94a3b8'>{d.CodigoEmpleado} · {d.Departamento}</div>
        </td>
        <td style='text-align:right'>Q {d.SalarioBase.ToString("N2", cult)}</td>
        <td style='text-align:right'>Q {d.Bonificacion250.ToString("N2", cult)}</td>
        <td style='text-align:right'>{(d.HorasExtraMonto > 0 ? $"Q {d.HorasExtraMonto.ToString("N2", cult)}" : "—")}</td>
        <td style='text-align:right;font-weight:700'>Q {d.TotalDevengado.ToString("N2", cult)}</td>
        <td style='text-align:right;color:#dc2626'>Q {d.CuotaIGSS.ToString("N2", cult)}</td>
        <td style='text-align:right;color:#dc2626'>{(d.ISR > 0 ? $"Q {d.ISR.ToString("N2", cult)}" : "—")}</td>
        <td style='text-align:right;color:#dc2626'>{(d.DescuentoPrestamo > 0 ? $"Q {d.DescuentoPrestamo.ToString("N2", cult)}" : "—")}</td>
        <td style='text-align:right;color:#dc2626;font-weight:700'>Q {d.TotalDeducciones.ToString("N2", cult)}</td>
        <td style='text-align:right;color:#16a34a;font-weight:700'>Q {d.SalarioNeto.ToString("N2", cult)}</td>
      </tr>"));

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ font-family: 'Segoe UI', Arial, sans-serif; font-size: 11px; color: #0f172a; }}
  .header {{ background: #0f172a; color: #fff; padding: 18px 24px; border-bottom: 3px solid #f59e0b; display:flex; justify-content:space-between; align-items:center; }}
  .header h1 {{ font-size: 17px; font-weight: 700; }}
  .header .sub {{ font-size: 12px; color: #94a3b8; margin-top: 3px; }}
  .kpi-row {{ display:flex; border-bottom: 1px solid #e2e8f0; }}
  .kpi {{ flex:1; padding: 12px 20px; border-right: 1px solid #e2e8f0; }}
  .kpi:last-child {{ border-right: none; }}
  .kpi label {{ font-size: 9px; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing:.6px; display:block; margin-bottom:4px; }}
  .kpi span {{ font-size: 16px; font-weight: 700; font-family: 'Consolas', monospace; }}
  .kpi.verde span {{ color: #16a34a; }}
  .kpi.rojo span {{ color: #dc2626; }}
  .patronal {{ background: #f0f9ff; border-bottom: 1px solid #bae6fd; padding: 8px 24px; font-size: 10px; color: #0369a1; display:flex; gap:20px; flex-wrap:wrap; }}
  .patronal strong {{ font-family: 'Consolas', monospace; }}
  table {{ width: 100%; border-collapse: collapse; margin-top: 0; }}
  thead th {{ background: #1e293b; color: #fff; padding: 8px 10px; font-size: 9.5px; font-weight: 700; text-transform: uppercase; letter-spacing:.5px; white-space:nowrap; }}
  thead th.r {{ text-align:right; }}
  tbody td {{ padding: 8px 10px; border-bottom: 1px solid #f1f5f9; font-size: 10.5px; vertical-align:middle; }}
  tfoot td {{ background: #0f172a; color: #fff; padding: 9px 10px; font-size: 11px; font-weight: 700; text-align:right; }}
  tfoot td:first-child {{ text-align:left; color:#94a3b8; font-size:9px; text-transform:uppercase; letter-spacing:.5px; }}
  tfoot td.tf-verde {{ color: #86efac; }}
  tfoot td.tf-danger {{ color: #fca5a5; }}
  .footer {{ margin-top: 30px; display:flex; justify-content:space-around; padding: 0 20px; }}
  .firma {{ text-align:center; }}
  .firma-line {{ border-top: 1px solid #0f172a; width:160px; margin: 0 auto 6px; }}
  .firma-label {{ font-size: 9px; color: #64748b; text-transform:uppercase; letter-spacing:.5px; }}
  .legal {{ margin-top:20px; font-size:9px; color:#94a3b8; text-align:center; border-top:1px solid #e2e8f0; padding-top:10px; }}
</style>
</head>
<body>

<div class='header'>
  <div>
    <h1>Planilla de Sueldos — {p.Periodo}</h1>
    <div class='sub'>{p.TotalEmpleados} empleados &nbsp;·&nbsp; Estado: {p.Estado} &nbsp;·&nbsp; Generado: {p.FechaGeneracion}</div>
  </div>
  <div style='font-size:11px;color:#94a3b8'>
    {(p.FechaPago != null ? $"Fecha pago: {p.FechaPago}" : "Pendiente de pago")}
  </div>
</div>

<div class='kpi-row'>
  <div class='kpi'>
    <label>Total devengado</label>
    <span>Q {p.TotalDevengado.ToString("N2", cult)}</span>
  </div>
  <div class='kpi rojo'>
    <label>Total deducciones</label>
    <span>Q {p.TotalDeducciones.ToString("N2", cult)}</span>
  </div>
  <div class='kpi verde'>
    <label>Total neto a pagar</label>
    <span>Q {p.TotalNeto.ToString("N2", cult)}</span>
  </div>
  <div class='kpi'>
    <label>Costo total empresa</label>
    <span>Q {costoEmpresa.ToString("N2", cult)}</span>
  </div>
</div>

<div class='patronal'>
  <span><strong>Aportes patronales:</strong></span>
  <span>IGSS 12.67% — <strong>Q {totalPatronal.ToString("N2", cult)}</strong></span>
  <span>IRTRA 1% — <strong>Q {(totalSalarios * 0.01m).ToString("N2", cult)}</strong></span>
  <span>INTECAP 1% — <strong>Q {(totalSalarios * 0.01m).ToString("N2", cult)}</strong></span>
</div>

<table>
  <thead>
    <tr>
      <th>#</th>
      <th>Empleado</th>
      <th class='r'>Salario</th>
      <th class='r'>Bono 250</th>
      <th class='r'>H.Extra</th>
      <th class='r'>Devengado</th>
      <th class='r'>IGSS Lab.</th>
      <th class='r'>ISR</th>
      <th class='r'>Préstamo</th>
      <th class='r'>Total Ded.</th>
      <th class='r'>Neto</th>
    </tr>
  </thead>
  <tbody>
    {filas}
  </tbody>
  <tfoot>
    <tr>
      <td colspan='2'>Totales</td>
      <td>Q {lista.Sum(d => d.SalarioBase).ToString("N2", cult)}</td>
      <td>Q {lista.Sum(d => d.Bonificacion250).ToString("N2", cult)}</td>
      <td>Q {lista.Sum(d => d.HorasExtraMonto).ToString("N2", cult)}</td>
      <td>Q {p.TotalDevengado.ToString("N2", cult)}</td>
      <td class='tf-danger'>Q {lista.Sum(d => d.CuotaIGSS).ToString("N2", cult)}</td>
      <td class='tf-danger'>Q {lista.Sum(d => d.ISR).ToString("N2", cult)}</td>
      <td class='tf-danger'>Q {lista.Sum(d => d.DescuentoPrestamo).ToString("N2", cult)}</td>
      <td class='tf-danger'>Q {p.TotalDeducciones.ToString("N2", cult)}</td>
      <td class='tf-verde'>Q {p.TotalNeto.ToString("N2", cult)}</td>
    </tr>
  </tfoot>
</table>

<div class='footer'>
  <div class='firma'>
    <div class='firma-line'></div>
    <div class='firma-label'>Elaborado por RRHH</div>
  </div>
  <div class='firma'>
    <div class='firma-line'></div>
    <div class='firma-label'>Revisado por Contabilidad</div>
  </div>
  <div class='firma'>
    <div class='firma-line'></div>
    <div class='firma-label'>Autorizado por Gerencia</div>
  </div>
</div>

<div class='legal'>
  Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} &nbsp;·&nbsp; 
  Confidencial — Uso interno únicamente &nbsp;·&nbsp; SistemaEmpleados RHH
</div>

</body>
</html>";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,RRHH")]
    public async Task<IActionResult> EliminarConcepto(int id)
    {
        var (success, message) = await _service.EliminarConceptoAsync(id);
        return Json(success ? ApiResponse.Ok(message) : ApiResponse.Fail(message));
    }
}