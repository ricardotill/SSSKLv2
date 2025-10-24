using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OldUserMigrationService(
    IOldUserMigrationRepository oldUserMigrationRepository,
    ILogger<OldUserMigrationService> logger) : IOldUserMigrationService
{
    public Task<int> GetCount() => oldUserMigrationRepository.GetCount();
    public async Task<OldUserMigration> GetMigrationById(Guid id)
    {
        logger.LogInformation($"{GetType()}: Get OldUserMigration with ID {id}");
        return await oldUserMigrationRepository.GetById(id);
    }
    
    public async Task<OldUserMigration> GetMigrationByUsername(string username)
    {
        logger.LogInformation($"{GetType()}: Get OldUserMigration with username {username}");
        return await oldUserMigrationRepository.GetByUsername(username);
    }

    public async Task<IEnumerable<OldUserMigration>> GetAll()
    {
        logger.LogInformation($"{GetType()}: Get All OldUserMigrations");
        return await oldUserMigrationRepository.GetAll();
    }

    public async Task CreateMigration(OldUserMigration obj)
    {
        logger.LogInformation($"{GetType()}: Create OldUserMigration for user {obj.Username} with saldo {obj.Saldo}");
        await oldUserMigrationRepository.Create(obj);
    }

    public async Task DeleteMigration(Guid id)
    {
        logger.LogInformation($"{GetType()}: Delete OldUserMigration for user with id {id}");
        await oldUserMigrationRepository.Delete(id);
    }
}