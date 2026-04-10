using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IPrestacionRepository : IRepository<Prestacion>
{
    Task<IEnumerable<Prestacion>> GetAllWithRelationsAsync();
    Task<Prestacion?> GetByIdWithRelationsAsync(int id);
    Task<IEnumerable<Prestacion>> GetByEmpleadoAsync(int empleadoId);
    Task<bool> ExistePrestacionAsync(int empleadoId, TipoPrestacion tipo, int periodo, int? excludeId = null);
}

public class PrestacionRepository : Repository<Prestacion>, IPrestacionRepository
{
    public PrestacionRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Prestacion> WithRelations() =>
        _dbSet
            .Include(p => p.Empleado)
            .ThenInclude(e => e.Departamento);

    public async Task<IEnumerable<Prestacion>> GetAllWithRelationsAsync() =>
        await WithRelations()
            .OrderByDescending(p => p.Periodo)
            .ThenBy(p => p.Empleado.PrimerApellido)
            .ToListAsync();

    public async Task<Prestacion?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Prestacion>> GetByEmpleadoAsync(int empleadoId) =>
        await WithRelations()
            .Where(p => p.EmpleadoId == empleadoId)
            .OrderByDescending(p => p.Periodo)
            .ToListAsync();

    public async Task<bool> ExistePrestacionAsync(
        int empleadoId, TipoPrestacion tipo, int periodo, int? excludeId = null) =>
        await _dbSet.AnyAsync(p =>
            p.EmpleadoId == empleadoId &&
            p.Tipo == tipo &&
            p.Periodo == periodo &&
            (excludeId == null || p.Id != excludeId));
}