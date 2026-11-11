using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Users;

internal static class ProfilePictures
{
    /// <summary>Deletes the user's current profile picture blob and record, if there is one.</summary>
    public static async Task Remove(ApplicationUser user, IUserImageRepository userImageRepository, IBlobStorageAgent blobAgent, UserManager<ApplicationUser> userManager)
    {
        if (user.ProfileImageId == null) return;

        var profileImage = await userImageRepository.GetById(user.ProfileImageId.Value);
        if (profileImage == null) return;

        await blobAgent.DeleteFileToBlobAsync(profileImage.FileName);
        user.ProfileImageId = null;
        await userImageRepository.Delete(profileImage);
        await userManager.UpdateAsync(user);
    }
}
