using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OldUserMigrationService(IOldUserMigrationRepository _oldUserMigrationRepository) : IOldUserMigrationService
{
    public async Task<OldUserMigration> GetMigrationById(Guid id)
    {
        return await _oldUserMigrationRepository.GetById(id);
    }
    
    public async Task<OldUserMigration> GetMigrationByUsername(string username)
    {
        return await _oldUserMigrationRepository.GetByUsername(username);
    }

    public async Task<IEnumerable<OldUserMigration>> GetAll()
    {
        return await _oldUserMigrationRepository.GetAll();
    }

    public async Task CreateProduct(OldUserMigration obj)
    {
        await _oldUserMigrationRepository.Create(obj);
    }

    public async Task DeleteProduct(Guid id)
    {
        await _oldUserMigrationRepository.Delete(id);
    }
}