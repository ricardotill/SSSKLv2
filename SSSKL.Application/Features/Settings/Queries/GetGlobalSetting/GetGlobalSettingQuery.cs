using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Settings.Queries.GetGlobalSetting;

/// <summary>Reads a setting. Write-only keys are never returned; sensitive keys only to admins.</summary>
public sealed record GetGlobalSettingQuery(string Key, bool IsAdmin) : IRequest<GlobalSettingDto>;
