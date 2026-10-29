using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Users.Commands.UpdateUser;

internal sealed class UpdateUserHandler(
    IApplicationUserRepository applicationUserRepository,
    UserManager<ApplicationUser> userManager) : IRequestHandler<UpdateUserCommand, ApplicationUserDetailedDto>
{
    public async Task<ApplicationUserDetailedDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await request.User.Resolve(applicationUserRepository);

        var isOwner = !string.IsNullOrWhiteSpace(request.RequesterUsername) &&
                      string.Equals(request.RequesterUsername, existing.UserName, StringComparison.OrdinalIgnoreCase);
        if (!request.IsAdmin && !isOwner)
            throw new UnauthorizedAccessException("Only admins can update other users.");

        var user = await userManager.FindByIdAsync(existing.Id)
                   ?? throw new NotFoundException("ApplicationUser not found");
        var dto = request.Changes;

        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
        {
            (await userManager.SetUserNameAsync(user, dto.UserName)).ThrowIfFailed();
            user.UserName = dto.UserName;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            (await userManager.SetEmailAsync(user, dto.Email)).ThrowIfFailed();
            user.Email = dto.Email;
        }

        if (dto.PhoneNumber != null && dto.PhoneNumber != user.PhoneNumber)
        {
            (await userManager.SetPhoneNumberAsync(user, dto.PhoneNumber)).ThrowIfFailed();
            user.PhoneNumber = dto.PhoneNumber;
        }

        var needsUpdate = false;
        if (dto.Name != null && dto.Name != user.Name)
        {
            user.Name = dto.Name;
            needsUpdate = true;
        }
        if (dto.Surname != null && dto.Surname != user.Surname)
        {
            user.Surname = dto.Surname;
            needsUpdate = true;
        }
        if (dto.Description != null && dto.Description != user.Description)
        {
            user.Description = dto.Description;
            needsUpdate = true;
        }
        if (needsUpdate)
        {
            (await userManager.UpdateAsync(user)).ThrowIfFailed();
        }

        // A reset token lets an admin set a password without knowing the current one
        if (!string.IsNullOrEmpty(dto.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            (await userManager.ResetPasswordAsync(user, token, dto.Password)).ThrowIfFailed();
        }

        // Replace all roles with the requested set
        if (dto.Roles != null && dto.Roles.Count > 0)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                (await userManager.RemoveFromRolesAsync(user, currentRoles)).ThrowIfFailed();
            }
            (await userManager.AddToRolesAsync(user, dto.Roles)).ThrowIfFailed();
        }

        var updated = await userManager.FindByIdAsync(user.Id) ?? user;
        var roles = await userManager.GetRolesAsync(updated);
        return updated.ToDetailedDto(roles);
    }
}
