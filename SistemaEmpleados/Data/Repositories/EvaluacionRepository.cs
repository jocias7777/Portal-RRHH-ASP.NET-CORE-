using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IEvaluacionRepository : IRepository<Evaluacion>
{
    Task<IEnumerable<Evaluacion>> GetAllWithRelationsAsync();
    Task<Evaluacion?> GetByIdWithRelationsAsync(int id);
}

public interface IKPIRepository : IRepository<KPI>
{
    Task<IEnumerable<KPI>> GetActivosAsync(int? puestoId = null);
}

public class EvaluacionRepository : Repository<Evaluacion>, IEvaluacionRepository
{
    public EvaluacionRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Evaluacion> WithRelations() =>
        _dbSet
            .Include(e => e.Empleado).ThenInclude(em => em.Departamento)
            .Include(e => e.Empleado).ThenInclude(em => em.Puesto)
            .Include(e => e.Evaluador)
            .Include(e => e.Resultados).ThenInclude(r => r.KPI);

    public async Task<IEnumerable<Evaluacion>> GetAllWithRelationsAsync() =>
        await WithRelations()
            .OrderByDescending(e => e.FechaEvaluacion)
            .ToListAsync();

    public async Task<Evaluacion?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(e => e.Id == id);
}

public class KPIRepository : Repository<KPI>, IKPIRepository
{
    public KPIRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<KPI>> GetActivosAsync(int? puestoId = null)
    {
        var query = _dbSet.Where(k => k.Activo);
        if (puestoId.HasValue)
            query = query.Where(k => k.PuestoId == null || k.PuestoId == puestoId);
        return await query.OrderBy(k => k.Nombre).ToListAsync();
    }
}
