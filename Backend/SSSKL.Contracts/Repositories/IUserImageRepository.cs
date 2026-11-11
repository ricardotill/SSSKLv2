using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface IUserImageRepository
{
    Task<UserImage?> GetById(Guid id);
    Task Add(UserImage image);
    Task Delete(UserImage image);
}
