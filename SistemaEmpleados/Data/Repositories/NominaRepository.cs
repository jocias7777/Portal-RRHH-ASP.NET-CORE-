using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IPlanillaRepository : IRepository<Planilla>
{
    Task<IEnumerable<Planilla>> GetAllWithTotalsAsync();
    Task<Planilla?> GetByIdWithDetallesAsync(int id);
    Task<bool> ExistePlanillaAsync(int mes, int anio, int? excludeId = null);
}

public class PlanillaRepository : Repository<Planilla>, IPlanillaRepository
{
    public PlanillaRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Planilla>> GetAllWithTotalsAsync() =>
        await _dbSet
            .OrderByDescending(p => p.Anio)
            .ThenByDescending(p => p.Mes)
            .ToListAsync();

    public async Task<Planilla?> GetByIdWithDetallesAsync(int id) =>
        await _dbSet
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Empleado)
                    .ThenInclude(e => e.Departamento)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Empleado)
                    .ThenInclude(e => e.Puesto)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<bool> ExistePlanillaAsync(int mes, int anio, int? excludeId = null) =>
        await _dbSet.AnyAsync(p =>
            p.Mes == mes && p.Anio == anio &&
            (excludeId == null || p.Id != excludeId));
}