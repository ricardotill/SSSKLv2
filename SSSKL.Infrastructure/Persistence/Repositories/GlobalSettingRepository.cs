using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Persistence.Repositories;

public class GlobalSettingRepository(ApplicationDbContext dbContext) : IGlobalSettingRepository
{
    public async Task<GlobalSetting?> GetByKey(string key)
    {
        return await dbContext.GlobalSetting.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task Upsert(string key, string value)
    {
        var setting = await dbContext.GlobalSetting.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new GlobalSetting
            {
                Key = key,
                Value = value,
                UpdatedOn = DateTime.UtcNow
            };
            dbContext.GlobalSetting.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedOn = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}
