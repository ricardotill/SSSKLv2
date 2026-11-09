using MediatR;

namespace SSSKL.Application.Features.RoleManagement.Commands.DeleteRole;

/// <summary>Deletes a role. System roles can't be deleted.</summary>
public sealed record DeleteRoleCommand(string Id) : IRequest<RoleResult>;
