using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IOldUserMigrationService
{
    public Task<OldUserMigration> GetMigrationById(Guid id);
    public Task<OldUserMigration> GetMigrationByUsername(string username);
    public Task<IEnumerable<OldUserMigration>> GetAll();
    public Task CreateMigration(OldUserMigration obj);
    public Task DeleteMigration(Guid id);
}