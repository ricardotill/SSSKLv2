namespace SSSKLv2.Data.DAL.Interfaces;

public interface IUserImageRepository
{
    Task<UserImage?> GetById(Guid id);
    Task Add(UserImage image);
    Task Delete(UserImage image);
}
