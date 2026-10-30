using MediatR;

namespace SSSKL.Application.Features.Users.Commands.DeleteProfilePicture;

/// <summary>Removes the user's profile picture. Does nothing when the user or picture doesn't exist.</summary>
public sealed record DeleteProfilePictureCommand(UserReference User) : IRequest;
