using MediatR;

namespace SSSKL.Application.Features.RoleManagement.Commands.UpdateRole;

/// <summary>Renames a role. System roles can't be renamed.</summary>
public sealed record UpdateRoleCommand(string Id, string Name) : IRequest<RoleResult>;
