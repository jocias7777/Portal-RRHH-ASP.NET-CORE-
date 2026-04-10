using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data.Repositories;

public interface IPlazaVacanteRepository : IRepository<PlazaVacante>
{
    Task<IEnumerable<PlazaVacante>> GetAllWithRelationsAsync();
    Task<PlazaVacante?> GetByIdWithRelationsAsync(int id);
}

public interface ICandidatoRepository : IRepository<Candidato>
{
    Task<IEnumerable<Candidato>> GetAllWithRelationsAsync();
    Task<Candidato?> GetByIdWithRelationsAsync(int id);
    Task<IEnumerable<Candidato>> GetByPlazaAsync(int plazaId);
}

public class PlazaVacanteRepository : Repository<PlazaVacante>, IPlazaVacanteRepository
{
    public PlazaVacanteRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<PlazaVacante> WithRelations() =>
        _dbSet
            .Include(p => p.Departamento)
            .Include(p => p.Puesto)
            .Include(p => p.Candidatos);

    public async Task<IEnumerable<PlazaVacante>> GetAllWithRelationsAsync() =>
        await WithRelations()
            .OrderByDescending(p => p.FechaPublicacion)
            .ToListAsync();

    public async Task<PlazaVacante?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(p => p.Id == id);
}

public class CandidatoRepository : Repository<Candidato>, ICandidatoRepository
{
    public CandidatoRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Candidato> WithRelations() =>
        _dbSet
            .Include(c => c.PlazaVacante)
                .ThenInclude(p => p.Departamento);

    public async Task<IEnumerable<Candidato>> GetAllWithRelationsAsync() =>
        await WithRelations()
            .OrderByDescending(c => c.FechaPostulacion)
            .ToListAsync();

    public async Task<Candidato?> GetByIdWithRelationsAsync(int id) =>
        await WithRelations().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Candidato>> GetByPlazaAsync(int plazaId) =>
        await WithRelations()
            .Where(c => c.PlazaVacanteId == plazaId)
            .OrderBy(c => c.Etapa)
            .ToListAsync();
}