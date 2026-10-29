using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Notifications.Commands.SendCustomNotification;

public sealed record SendCustomNotificationCommand(CreateCustomNotificationDto Notification) : IRequest;
