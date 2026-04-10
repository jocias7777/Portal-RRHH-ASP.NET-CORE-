using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class EmpleadoService : IEmpleadoService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmpleadoRepository _repo;
    private readonly IUnitOfWork _uow;

    public EmpleadoService(
        ApplicationDbContext context,
        IEmpleadoRepository repo,
        IUnitOfWork uow)
    {
        _context = context;
        _repo = repo;
        _uow = uow;
    }

    public async Task<DataTablesResponse<EmpleadoListViewModel>> GetDataTablesAsync(DataTablesRequest req)
    {
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.Puesto)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(e =>
                e.PrimerNombre.ToLower().Contains(s) ||
                e.PrimerApellido.ToLower().Contains(s) ||
                e.SegundoApellido!.ToLower().Contains(s) ||
                e.Codigo.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                e.Departamento.Nombre.ToLower().Contains(s));
        }

        if (req.DepartamentoId.HasValue)
            query = query.Where(e => e.DepartamentoId == req.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EstadoEmpleado>(req.Estado, out var estado))
            query = query.Where(e => e.Estado == estado);

        var total = await query.CountAsync();

        // Ordenamiento
        query = req.OrderColumn switch
        {
            "codigo" => req.OrderDir == "asc" ? query.OrderBy(e => e.Codigo) : query.OrderByDescending(e => e.Codigo),
            "nombre" => req.OrderDir == "asc" ? query.OrderBy(e => e.PrimerApellido) : query.OrderByDescending(e => e.PrimerApellido),
            "departamento" => req.OrderDir == "asc" ? query.OrderBy(e => e.Departamento.Nombre) : query.OrderByDescending(e => e.Departamento.Nombre),
            "salario" => req.OrderDir == "asc" ? query.OrderBy(e => e.SalarioBase) : query.OrderByDescending(e => e.SalarioBase),
            _ => query.OrderBy(e => e.PrimerApellido)
        };

        var data = await query
            .Skip(req.Start)
            .Take(req.Length)
            .Select(e => new EmpleadoListViewModel
            {
                Id = e.Id,
                Codigo = e.Codigo,
                NombreCompleto = $"{e.PrimerNombre} {e.SegundoNombre} {e.PrimerApellido} {e.SegundoApellido}".Replace("  ", " ").Trim(),
                Iniciales = $"{e.PrimerNombre[0]}{e.PrimerApellido[0]}".ToUpper(),
                FotoUrl = e.FotoUrl,
                Departamento = e.Departamento.Nombre,
                Puesto = e.Puesto.Nombre,
                TipoContrato = e.TipoContrato.ToString(),
                Estado = e.Estado.ToString(),
                Email = e.Email,
                FechaIngreso = e.FechaIngreso.ToString("dd/MM/yyyy"),
                SalarioBase = e.SalarioBase
            })
            .ToListAsync();

        return new DataTablesResponse<EmpleadoListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    public async Task<EmpleadoDetalleViewModel?> GetByIdAsync(int id)
    {
        var e = await _repo.GetByIdWithRelationsAsync(id);
        if (e == null) return null;

        return new EmpleadoDetalleViewModel
        {
            Id = e.Id,
            Codigo = e.Codigo,
            PrimerNombre = e.PrimerNombre,
            SegundoNombre = e.SegundoNombre,
            PrimerApellido = e.PrimerApellido,
            SegundoApellido = e.SegundoApellido,
            FechaNacimiento = e.FechaNacimiento,
            Genero = e.Genero,
            CUI = e.CUI,
            NIT = e.NIT,
            NumeroIGSS = e.NumeroIGSS,
            NumeroIRTRA = e.NumeroIRTRA,
            FotoUrl = e.FotoUrl,
            Telefono = e.Telefono,
            Email = e.Email,
            FechaIngreso = e.FechaIngreso,
            FechaSalida = e.FechaSalida,
            Estado = e.Estado,
            TipoContrato = e.TipoContrato,
            SalarioBase = e.SalarioBase,
            Observaciones = e.Observaciones,
            DepartamentoId = e.DepartamentoId,
            PuestoId = e.PuestoId,
            NombreCompleto = e.NombreCompleto,
            NombreDepartamento = e.Departamento.Nombre,
            NombrePuesto = e.Puesto.Nombre,
            AniosServicio = (int)((DateTime.Today - e.FechaIngreso).TotalDays / 365)
        };
    }

    public async Task<(bool success, string message, int id)> CreateAsync(EmpleadoViewModel vm)
    {
        if (await _repo.ExisteCodigoAsync(vm.CUI))
        {
            // Autogenerar código
        }

        if (await _repo.ExisteCUIAsync(vm.CUI))
            return (false, "Ya existe un empleado con ese CUI/DPI.", 0);

        var codigo = await GenerateNextCodigoAsync();

        var empleado = new Empleado
        {
            Codigo = codigo,
            PrimerNombre = vm.PrimerNombre.Trim(),
            SegundoNombre = vm.SegundoNombre?.Trim(),
            PrimerApellido = vm.PrimerApellido.Trim(),
            SegundoApellido = vm.SegundoApellido?.Trim(),
            FechaNacimiento = vm.FechaNacimiento,
            Genero = vm.Genero,
            CUI = vm.CUI.Trim(),
            NIT = vm.NIT?.Trim(),
            NumeroIGSS = vm.NumeroIGSS?.Trim(),
            NumeroIRTRA = vm.NumeroIRTRA?.Trim(),
            FotoUrl = vm.FotoUrl,
            Telefono = vm.Telefono?.Trim(),
            Email = vm.Email.Trim().ToLower(),
            FechaIngreso = vm.FechaIngreso,
            Estado = vm.Estado,
            TipoContrato = vm.TipoContrato,
            SalarioBase = vm.SalarioBase,
            Observaciones = vm.Observaciones,
            DepartamentoId = vm.DepartamentoId,
            PuestoId = vm.PuestoId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(empleado);
        await _uow.SaveChangesAsync();

        return (true, $"Empleado {empleado.NombreCompleto} creado correctamente.", empleado.Id);
    }

    public async Task<(bool success, string message)> UpdateAsync(int id, EmpleadoViewModel vm)
    {
        var empleado = await _repo.GetByIdAsync(id);
        if (empleado == null) return (false, "Empleado no encontrado.");

        if (await _repo.ExisteCUIAsync(vm.CUI, id))
            return (false, "Ya existe otro empleado con ese CUI/DPI.");

        empleado.PrimerNombre = vm.PrimerNombre.Trim();
        empleado.SegundoNombre = vm.SegundoNombre?.Trim();
        empleado.PrimerApellido = vm.PrimerApellido.Trim();
        empleado.SegundoApellido = vm.SegundoApellido?.Trim();
        empleado.FechaNacimiento = vm.FechaNacimiento;
        empleado.Genero = vm.Genero;
        empleado.CUI = vm.CUI.Trim();
        empleado.NIT = vm.NIT?.Trim();
        empleado.NumeroIGSS = vm.NumeroIGSS?.Trim();
        empleado.NumeroIRTRA = vm.NumeroIRTRA?.Trim();
        empleado.FotoUrl = vm.FotoUrl;
        empleado.Telefono = vm.Telefono?.Trim();
        empleado.Email = vm.Email.Trim().ToLower();
        empleado.FechaIngreso = vm.FechaIngreso;
        empleado.FechaSalida = vm.FechaSalida;
        empleado.Estado = vm.Estado;
        empleado.TipoContrato = vm.TipoContrato;
        empleado.SalarioBase = vm.SalarioBase;
        empleado.Observaciones = vm.Observaciones;
        empleado.DepartamentoId = vm.DepartamentoId;
        empleado.PuestoId = vm.PuestoId;
        empleado.UpdatedAt = DateTime.UtcNow;

        _repo.Update(empleado);
        await _uow.SaveChangesAsync();

        return (true, $"Empleado {empleado.NombreCompleto} actualizado correctamente.");
    }

    public async Task<(bool success, string message)> DeleteAsync(int id)
    {
        var empleado = await _repo.GetByIdAsync(id);
        if (empleado == null) return (false, "Empleado no encontrado.");

        empleado.IsDeleted = true;
        empleado.Estado = EstadoEmpleado.Baja;
        empleado.FechaSalida = DateTime.Today;
        empleado.UpdatedAt = DateTime.UtcNow;

        _repo.Update(empleado);
        await _uow.SaveChangesAsync();

        return (true, $"Empleado dado de baja correctamente.");
    }

    public async Task<IEnumerable<object>> SearchForGlobalAsync(string term)
    {
        var results = await _repo.SearchAsync(term);
        return results.Select(e => new
        {
            id = e.Id,
            texto = e.NombreCompleto,
            sub = e.Departamento?.Nombre ?? "",
            url = $"/Personal/Detalle/{e.Id}",
            icono = "fa-id-badge",
            color = "#4A90D9"
        });
    }

    public async Task<string> GenerateNextCodigoAsync()
    {
        var last = await _context.Empleados
            .IgnoreQueryFilters()
            .OrderByDescending(e => e.Id)
            .Select(e => e.Codigo)
            .FirstOrDefaultAsync();

        if (last == null) return "EMP-0001";

        var num = int.TryParse(last.Replace("EMP-", ""), out var n) ? n + 1 : 1;
        return $"EMP-{num:D4}";
    }
}