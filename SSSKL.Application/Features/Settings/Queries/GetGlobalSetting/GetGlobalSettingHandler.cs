using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Settings.Queries.GetGlobalSetting;

internal sealed class GetGlobalSettingHandler(IGlobalSettingRepository globalSettingRepository)
    : IRequestHandler<GetGlobalSettingQuery, GlobalSettingDto>
{
    public async Task<GlobalSettingDto> Handle(GetGlobalSettingQuery request, CancellationToken cancellationToken)
    {
        if (GlobalSettingsKeys.WriteOnlyKeys.Contains(request.Key))
            throw new UnauthorizedAccessException("This setting can't be read through the API.");

        if (GlobalSettingsKeys.SensitiveKeys.Contains(request.Key) && !request.IsAdmin)
            throw new UnauthorizedAccessException("Only admins can read this setting.");

        var setting = await globalSettingRepository.GetByKey(request.Key)
                      ?? throw new NotFoundException("Setting not found");

        return new GlobalSettingDto
        {
            Key = setting.Key,
            Value = setting.Value,
            UpdatedOn = setting.UpdatedOn
        };
    }
}
