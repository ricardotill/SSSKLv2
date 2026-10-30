using MediatR;

namespace SSSKL.Application.Features.Orders.Commands.DeleteOrder;

/// <summary>Admins can delete any order; other users only their own.</summary>
public sealed record DeleteOrderCommand(Guid Id, string? UserId, bool IsAdmin) : IRequest;
