using MediatR;

namespace SSSKL.Application.Features.RoleManagement.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name) : IRequest<RoleResult>;
