using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

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
