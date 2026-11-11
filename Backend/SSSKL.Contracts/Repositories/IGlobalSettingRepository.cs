using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface IGlobalSettingRepository
{
    Task<GlobalSetting?> GetByKey(string key);
    Task Upsert(string key, string value);
}
