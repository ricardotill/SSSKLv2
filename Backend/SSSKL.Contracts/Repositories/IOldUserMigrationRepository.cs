using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface IOldUserMigrationRepository
{
    Task<int> GetCount();
    public Task<OldUserMigration> GetById(Guid id);
    public Task<OldUserMigration> GetByUsername(string username);
    public Task<IEnumerable<OldUserMigration>> GetAll();
    public Task Create(OldUserMigration obj);
    public Task Delete(Guid id);
}