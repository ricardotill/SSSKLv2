using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OldUserMigrationService(
    IOldUserMigrationRepository _oldUserMigrationRepository,
    ILogger<OldUserMigrationService> _logger) : IOldUserMigrationService
{
    public async Task<OldUserMigration> GetMigrationById(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Get OldUserMigration with ID {id}");
        return await _oldUserMigrationRepository.GetById(id);
    }
    
    public async Task<OldUserMigration> GetMigrationByUsername(string username)
    {
        _logger.LogInformation($"{GetType()}: Get OldUserMigration with username {username}");
        return await _oldUserMigrationRepository.GetByUsername(username);
    }

    public async Task<IEnumerable<OldUserMigration>> GetAll()
    {
        _logger.LogInformation($"{GetType()}: Get All OldUserMigrations");
        return await _oldUserMigrationRepository.GetAll();
    }

    public async Task CreateMigration(OldUserMigration obj)
    {
        _logger.LogInformation($"{GetType()}: Create OldUserMigration for user {obj.Username} with saldo {obj.Saldo}");
        await _oldUserMigrationRepository.Create(obj);
    }

    public async Task DeleteMigration(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Delete OldUserMigration for user with id {id}");
        await _oldUserMigrationRepository.Delete(id);
    }
}