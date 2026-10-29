namespace SSSKLv2.Data.DAL.Interfaces;

public interface IGlobalSettingRepository
{
    Task<GlobalSetting?> GetByKey(string key);
    Task Upsert(string key, string value);
}
