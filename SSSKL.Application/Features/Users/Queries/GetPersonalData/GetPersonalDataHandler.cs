using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Users.Queries.GetPersonalData;

internal sealed class GetPersonalDataHandler(UserManager<ApplicationUser> userManager, ILogger<GetPersonalDataHandler> logger)
    : IRequestHandler<GetPersonalDataQuery, IReadOnlyDictionary<string, string>>
{
    public async Task<IReadOnlyDictionary<string, string>> Handle(GetPersonalDataQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
                   ?? throw new NotFoundException($"Unable to load user with ID '{request.UserId}'.");

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("User with ID '{UserId}' requested personal data.", user.Id);

        return typeof(ApplicationUser).GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)))
            .ToDictionary(p => p.Name, p => p.GetValue(user)?.ToString() ?? "null");
    }
}
