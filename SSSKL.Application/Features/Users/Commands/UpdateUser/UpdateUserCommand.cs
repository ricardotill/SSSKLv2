using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Commands.UpdateUser;

/// <summary>Updates a user. Admins can change anything; a user updating themselves only the profile fields the API passes.</summary>
/// <param name="RequesterUsername">The signed-in user; must be the user being updated unless <paramref name="IsAdmin"/>.</param>
public sealed record UpdateUserCommand(UserReference User, ApplicationUserUpdateDto Changes, string? RequesterUsername, bool IsAdmin)
    : IRequest<ApplicationUserDetailedDto>;
