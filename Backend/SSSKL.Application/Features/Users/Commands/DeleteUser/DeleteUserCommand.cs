using MediatR;

namespace SSSKL.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(UserReference User) : IRequest;
