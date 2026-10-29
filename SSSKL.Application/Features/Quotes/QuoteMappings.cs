using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Quotes;

public static class QuoteMappings
{
    public static QuoteDto ToDto(this Quote quote) => new()
    {
        Id = quote.Id,
        Text = quote.Text,
        DateSaid = quote.DateSaid,
        CreatedOn = quote.CreatedOn,
        CreatedBy = ToUserDto(quote.CreatedBy),
        Authors = quote.Authors.Select(a => new QuoteAuthorDto
        {
            Id = a.Id,
            ApplicationUserId = a.ApplicationUserId,
            ApplicationUser = ToUserDto(a.ApplicationUser),
            CustomName = a.CustomName
        }).ToList(),
        VisibleToRoles = quote.VisibleToRoles.Select(r => r.Name ?? string.Empty).ToList()
    };

    private static ApplicationUserDto? ToUserDto(ApplicationUser? user)
    {
        if (user == null) return null;
        return new ApplicationUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{user.ProfileImageId}" : null
        };
    }
}
