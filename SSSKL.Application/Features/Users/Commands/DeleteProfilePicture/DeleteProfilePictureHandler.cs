using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Users.Commands.DeleteProfilePicture;

internal sealed class DeleteProfilePictureHandler(
    IApplicationUserRepository applicationUserRepository,
    UserManager<ApplicationUser> userManager,
    IUserImageRepository userImageRepository,
    IBlobStorageAgent blobAgent) : IRequestHandler<DeleteProfilePictureCommand>
{
    public async Task Handle(DeleteProfilePictureCommand request, CancellationToken cancellationToken)
    {
        // The "me" route resolves the username first and 404s for an unknown user; the admin route is a no-op.
        var userId = request.User.Id ?? (await request.User.Resolve(applicationUserRepository)).Id;
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return;

        await ProfilePictures.Remove(user, userImageRepository, blobAgent, userManager);
    }
}
