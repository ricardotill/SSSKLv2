using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Users.Commands.DeleteUser;

internal sealed class DeleteUserHandler(
    IApplicationUserRepository applicationUserRepository,
    UserManager<ApplicationUser> userManager) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await request.User.Resolve(applicationUserRepository);
        var user = await userManager.FindByIdAsync(existing.Id)
                   ?? throw new NotFoundException("ApplicationUser not found");

        (await userManager.DeleteAsync(user)).ThrowIfFailed();
    }
}
