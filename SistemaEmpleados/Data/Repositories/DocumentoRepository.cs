using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IDocumentoRepository : IRepository<Documento>
{
    Task<IEnumerable<Documento>> GetAllWithRelationsAsync();
    Task<Documento?> GetByIdWithRelationsAsync(int id);
    Task<IEnumerable<Documento>> GetByEmpleadoAsync(int empleadoId);
    Task<IEnumerable<Documento>> GetExpiringAsync(int dias);
    Task<bool> ExisteTituloAsync(string titulo, int? excludeId = null, int? empleadoId = null);
    Task<int> CountActivosAsync();
    Task<IEnumerable<Documento>> SearchAsync(string term);
}

public class DocumentoRepository : Repository<Documento>, IDocumentoRepository
{
    public DocumentoRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Documento> WithRelations() =>
        _dbSet
            .Include(d => d.Empleado)
            .ThenInclude(e => e.Departamento);

    public async Task<IEnumerable<Documento>> GetAllWithRelationsAsync() =>
        await WithRelations().OrderByDescending(d => d.CreatedAt).ToListAsync();

    public async Task<Documento?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<IEnumerable<Documento>> GetByEmpleadoAsync(int empleadoId) =>
        await WithRelations()
            .Where(d => d.EmpleadoId == empleadoId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Documento>> GetExpiringAsync(int dias)
    {
        var fechaLimite = DateTime.Today.AddDays(dias);
        return await _dbSet
            .Include(d => d.Empleado)
            .Where(d => !d.IsDeleted && d.FechaExpiracion != null && d.FechaExpiracion <= fechaLimite)
            .OrderBy(d => d.FechaExpiracion)
            .ToListAsync();
    }

    public async Task<bool> ExisteTituloAsync(string titulo, int? excludeId = null, int? empleadoId = null) =>
        await _dbSet.AnyAsync(d =>
            d.Titulo == titulo && 
            (empleadoId == null || d.EmpleadoId == empleadoId) &&
            (excludeId == null || d.Id != excludeId));

    public async Task<int> CountActivosAsync() =>
        await _dbSet.CountAsync(d => d.Estado == EstadoDocumento.Activo);

    public async Task<IEnumerable<Documento>> SearchAsync(string term) =>
        await WithRelations()
            .Where(d =>
                d.Titulo.Contains(term) ||
                d.Descripcion!.Contains(term) ||
                d.Empleado.PrimerNombre.Contains(term) ||
                d.Empleado.PrimerApellido.Contains(term))
            .Take(10)
            .ToListAsync();
}