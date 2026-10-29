using MediatR;

namespace SSSKL.Application.Features.Users.Commands.UpdateProfilePicture;

/// <summary>Crops the image to a 400x400 PNG and replaces the user's profile picture.</summary>
public sealed record UpdateProfilePictureCommand(UserReference User, Stream Image) : IRequest;
