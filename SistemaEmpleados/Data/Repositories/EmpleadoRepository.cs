using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IEmpleadoRepository : IRepository<Empleado>
{
    Task<IEnumerable<Empleado>> GetAllWithRelationsAsync();
    Task<Empleado?> GetByIdWithRelationsAsync(int id);
    Task<IEnumerable<Empleado>> GetByDepartamentoAsync(int departamentoId);
    Task<bool> ExisteCodigoAsync(string codigo, int? excludeId = null);
    Task<bool> ExisteCUIAsync(string cui, int? excludeId = null);
    Task<int> CountActivosAsync();
    Task<IEnumerable<Empleado>> SearchAsync(string term);
}

public class EmpleadoRepository : Repository<Empleado>, IEmpleadoRepository
{
    public EmpleadoRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Empleado> WithRelations() =>
        _dbSet
            .Include(e => e.Departamento)
            .Include(e => e.Puesto);

    public async Task<IEnumerable<Empleado>> GetAllWithRelationsAsync() =>
        await WithRelations().OrderBy(e => e.PrimerApellido).ToListAsync();

    public async Task<Empleado?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<Empleado>> GetByDepartamentoAsync(int departamentoId) =>
        await WithRelations().Where(e => e.DepartamentoId == departamentoId).ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excludeId = null) =>
        await _dbSet.AnyAsync(e =>
            e.Codigo == codigo && (excludeId == null || e.Id != excludeId));

    public async Task<bool> ExisteCUIAsync(string cui, int? excludeId = null) =>
        await _dbSet.AnyAsync(e =>
            e.CUI == cui && (excludeId == null || e.Id != excludeId));

    public async Task<int> CountActivosAsync() =>
        await _dbSet.CountAsync(e => e.Estado == EstadoEmpleado.Activo);

    public async Task<IEnumerable<Empleado>> SearchAsync(string term) =>
        await WithRelations()
            .Where(e =>
                e.PrimerNombre.Contains(term) ||
                e.PrimerApellido.Contains(term) ||
                e.SegundoApellido!.Contains(term) ||
                e.Codigo.Contains(term) ||
                e.CUI.Contains(term) ||
                e.Email.Contains(term))
            .Take(10)
            .ToListAsync();
}