using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IOldUserMigrationService
{
    public Task<OldUserMigration> GetProductById(Guid id);
    public Task<IEnumerable<OldUserMigration>> GetAll();
    public Task CreateProduct(OldUserMigration obj);
    public Task DeleteProduct(Guid id);
}