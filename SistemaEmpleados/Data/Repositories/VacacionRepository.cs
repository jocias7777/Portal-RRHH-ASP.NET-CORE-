using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IVacacionRepository : IRepository<Vacacion>
{
    Task<IEnumerable<Vacacion>> GetAllWithRelationsAsync();
    Task<Vacacion?> GetByIdWithRelationsAsync(int id);
    Task<int> GetDiasUsadosEnAnioAsync(int empleadoId, int anio);
    Task<IEnumerable<Vacacion>> GetByEmpleadoAsync(int empleadoId);
}

public interface IAusenciaRepository : IRepository<Ausencia>
{
    Task<IEnumerable<Ausencia>> GetAllWithRelationsAsync();
    Task<Ausencia?> GetByIdWithRelationsAsync(int id);
}

public class VacacionRepository : Repository<Vacacion>, IVacacionRepository
{
    public VacacionRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Vacacion> WithRelations() =>
        _dbSet.Include(v => v.Empleado).ThenInclude(e => e.Departamento);

    public async Task<IEnumerable<Vacacion>> GetAllWithRelationsAsync() =>
        await WithRelations().OrderByDescending(v => v.FechaSolicitud).ToListAsync();

    public async Task<Vacacion?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(v => v.Id == id);

    public async Task<int> GetDiasUsadosEnAnioAsync(int empleadoId, int anio) =>
        await _dbSet
            .Where(v => v.EmpleadoId == empleadoId &&
                        v.FechaInicio.Year == anio &&
                        v.Estado == EstadoVacacion.Aprobado)
            .SumAsync(v => v.DiasHabiles);

    public async Task<IEnumerable<Vacacion>> GetByEmpleadoAsync(int empleadoId) =>
        await WithRelations()
            .Where(v => v.EmpleadoId == empleadoId)
            .OrderByDescending(v => v.FechaSolicitud)
            .ToListAsync();
}

public class AusenciaRepository : Repository<Ausencia>, IAusenciaRepository
{
    public AusenciaRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Ausencia> WithRelations() =>
        _dbSet.Include(a => a.Empleado).ThenInclude(e => e.Departamento);

    public async Task<IEnumerable<Ausencia>> GetAllWithRelationsAsync() =>
        await WithRelations().OrderByDescending(a => a.FechaInicio).ToListAsync();

    public async Task<Ausencia?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(a => a.Id == id);
}
