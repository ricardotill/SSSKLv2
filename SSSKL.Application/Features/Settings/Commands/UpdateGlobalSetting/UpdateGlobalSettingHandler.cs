using Ganss.Xss;
using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;

namespace SSSKL.Application.Features.Settings.Commands.UpdateGlobalSetting;

internal sealed class UpdateGlobalSettingHandler(IGlobalSettingRepository globalSettingRepository)
    : IRequestHandler<UpdateGlobalSettingCommand>
{
    public Task Handle(UpdateGlobalSettingCommand request, CancellationToken cancellationToken)
    {
        // Sensitive keys (API keys, passwords) are stored verbatim
        var value = GlobalSettingsKeys.SensitiveKeys.Contains(request.Key)
            ? request.Value
            : new HtmlSanitizer().Sanitize(request.Value);

        return globalSettingRepository.Upsert(request.Key, value);
    }
}
