namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOldUserMigrationRepository
{
    public Task<OldUserMigration> GetById(Guid id);
    public Task<OldUserMigration> GetByUsername(string username);
    public Task<IEnumerable<OldUserMigration>> GetAll();
    public Task Create(OldUserMigration obj);
    public Task Delete(Guid id);
}