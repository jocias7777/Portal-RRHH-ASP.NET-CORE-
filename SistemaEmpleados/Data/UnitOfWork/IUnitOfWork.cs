using SistemaEmpleados.Data.Repositories;

namespace SistemaEmpleados.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}