using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IAsistenciaRepository : IRepository<Asistencia>
{
    Task<IEnumerable<Asistencia>> GetAllWithRelationsAsync();
    Task<Asistencia?> GetByIdWithRelationsAsync(int id);
    Task<IEnumerable<Asistencia>> GetByEmpleadoAsync(int empleadoId, DateTime? desde = null, DateTime? hasta = null);
    Task<IEnumerable<Asistencia>> GetByFechaAsync(DateTime fecha);
    Task<bool> ExisteAsistenciaAsync(int empleadoId, DateTime fecha, int? excludeId = null);
}

public class AsistenciaRepository : Repository<Asistencia>, IAsistenciaRepository
{
    public AsistenciaRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Asistencia> WithRelations() =>
        _dbSet
            .Include(a => a.Empleado)
                .ThenInclude(e => e.Departamento)
            .Include(a => a.Horario);

    public async Task<IEnumerable<Asistencia>> GetAllWithRelationsAsync() =>
        await WithRelations()
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();

    public async Task<Asistencia?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Asistencia>> GetByEmpleadoAsync(
        int empleadoId, DateTime? desde = null, DateTime? hasta = null)
    {
        var query = WithRelations().Where(a => a.EmpleadoId == empleadoId);
        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);
        return await query.OrderByDescending(a => a.Fecha).ToListAsync();
    }

    public async Task<IEnumerable<Asistencia>> GetByFechaAsync(DateTime fecha) =>
        await WithRelations()
            .Where(a => a.Fecha.Date == fecha.Date)
            .ToListAsync();

    public async Task<bool> ExisteAsistenciaAsync(int empleadoId, DateTime fecha, int? excludeId = null) =>
        await _dbSet.AnyAsync(a =>
            a.EmpleadoId == empleadoId &&
            a.Fecha.Date == fecha.Date &&
            (excludeId == null || a.Id != excludeId));
}
