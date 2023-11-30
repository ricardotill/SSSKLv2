using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OldUserMigrationRepository : IOldUserMigrationRepository
{
    public Task<OldUserMigration> GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OldUserMigration>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task Create(OldUserMigration obj)
    {
        throw new NotImplementedException();
    }

    public Task Update(OldUserMigration obj)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}