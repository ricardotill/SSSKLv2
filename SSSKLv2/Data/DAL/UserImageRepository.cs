using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

// Shares the scoped context with UserManager, so changes to the tracked user are saved together with the image.
public class UserImageRepository(ApplicationDbContext dbContext) : IUserImageRepository
{
    public async Task<UserImage?> GetById(Guid id)
    {
        return await dbContext.UserImage.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task Add(UserImage image)
    {
        dbContext.UserImage.Add(image);
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(UserImage image)
    {
        dbContext.UserImage.Remove(image);
        await dbContext.SaveChangesAsync();
    }
}
