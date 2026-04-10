using Microsoft.EntityFrameworkCore.Storage;
using SistemaEmpleados.Data.Repositories;

namespace SistemaEmpleados.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
            _repositories[type] = new Repository<T>(_context);
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}