using MediatR;

namespace SSSKL.Application.Features.Settings.Commands.UpdateGlobalSetting;

/// <summary>Creates or updates a setting. Values of non-sensitive keys are HTML-sanitized.</summary>
public sealed record UpdateGlobalSettingCommand(string Key, string Value) : IRequest;
