using MediatR;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SSSKL.Application.Util;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Users.Commands.UpdateProfilePicture;

internal sealed class UpdateProfilePictureHandler(
    IApplicationUserRepository applicationUserRepository,
    UserManager<ApplicationUser> userManager,
    IUserImageRepository userImageRepository,
    IBlobStorageAgent blobAgent) : IRequestHandler<UpdateProfilePictureCommand>
{
    public async Task Handle(UpdateProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var existing = await request.User.Resolve(applicationUserRepository);
        var user = await userManager.FindByIdAsync(existing.Id)
                   ?? throw new NotFoundException("User not found");

        using var image = await SafeImageLoader.LoadAsync(request.Image, cancellationToken);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(400, 400),
            Mode = ResizeMode.Crop
        }));

        using var outStream = new MemoryStream();
        await image.SaveAsPngAsync(outStream, cancellationToken);
        outStream.Position = 0;

        var fileName = $"profile-{user.Id}-{Guid.NewGuid()}.png";
        var blobItem = await blobAgent.UploadFileToBlobAsync(fileName, "image/png", outStream);

        await ProfilePictures.Remove(user, userImageRepository, blobAgent, userManager);

        var userImage = UserImage.ToUserImage(blobItem);
        userImage.User = user;
        user.ProfileImageId = userImage.Id;

        await userImageRepository.Add(userImage);
        await userManager.UpdateAsync(user);
    }
}
