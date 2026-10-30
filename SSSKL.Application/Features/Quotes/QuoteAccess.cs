using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Quotes;

/// <summary>Access rules shared by the quote handlers.</summary>
internal sealed class QuoteAccess(IGlobalSettingRepository globalSettingRepository, IUserRoleProvider roleProvider)
{
    private const string GlobalSettingKey = "QuotesFeatureAllowedRoles";

    /// <summary>Throws when the quotes feature is restricted to roles the user doesn't have.</summary>
    public async Task EnsureUserHasAccess(string? userId)
    {
        var setting = await globalSettingRepository.GetByKey(GlobalSettingKey);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            // No setting configured: the feature isn't restricted.
            return;
        }

        if (userId == null) throw new UnauthorizedAccessException("Authentication required for Quotes feature.");

        var allowedRoles = setting.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var userRoles = await roleProvider.GetRoles(userId);

        if (!userRoles.Contains(Roles.Admin) && !allowedRoles.Any(r => userRoles.Contains(r)))
        {
            throw new UnauthorizedAccessException("You do not have the required roles to use the Quotes feature.");
        }
    }

    /// <summary>Only the creator, an admin or one of the authors may change or delete a quote.</summary>
    public async Task EnsureUserCanModify(Quote quote, string userId)
    {
        var userRoles = await roleProvider.GetRoles(userId);
        if (userRoles.Contains(Roles.Admin)) return;

        if (quote.CreatedById == userId) return;

        if (quote.Authors.Any(a => a.ApplicationUserId == userId)) return;

        throw new UnauthorizedAccessException("Only the creator, an admin, or an author of the quote can modify or delete it.");
    }

    public static async Task<Quote> GetExisting(IQuoteRepository quoteRepository, Guid id) =>
        await quoteRepository.GetById(id) ?? throw new NotFoundException("Quote not found");

    public static void ApplyAuthors(Quote quote, IEnumerable<QuoteAuthorCreateDto>? authors)
    {
        quote.Authors.Clear();
        if (authors == null) return;

        foreach (var authorDto in authors)
        {
            quote.Authors.Add(new QuoteAuthor
            {
                ApplicationUserId = authorDto.ApplicationUserId,
                CustomName = authorDto.CustomName
            });
        }
    }
}
