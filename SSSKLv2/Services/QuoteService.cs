using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class QuoteService(IQuoteRepository quoteRepository, 
    IApplicationUserService applicationUserService, 
    ApplicationDbContext dbContext,
    INotificationService notificationService) : IQuoteService
{
    private const string GlobalSettingKey = "QuotesFeatureAllowedRoles";

    public async Task<IEnumerable<QuoteDto>> GetQuotesAsync(int skip = 0, int take = 15, string? userId = null, string? targetUserId = null)
    {
        await EnsureUserHasAccess(userId);

        IList<string>? userRoles = null;
        bool isAdmin = false;
        if (userId != null)
        {
            userRoles = await applicationUserService.GetUserRoles(userId);
            isAdmin = userRoles.Contains(Roles.Admin);
        }

        var quotes = await quoteRepository.GetAll(skip, take, userRoles, isAdmin, targetUserId);
        var quoteDtos = quotes.Select(MapToDto).ToList();

        var quoteIds = quoteDtos.Select(q => q.Id).ToList();
        
        // Fetch Vote counts and user's voted state
        var voteInfo = await dbContext.QuoteVote
            .Where(v => quoteIds.Contains(v.QuoteId))
            .GroupBy(v => v.QuoteId)
            .Select(g => new
            {
                QuoteId = g.Key,
                Count = g.Count(),
                HasVoted = userId != null && g.Any(v => v.UserId == userId)
            })
            .ToDictionaryAsync(x => x.QuoteId, x => x);

        // Fetch Comment counts (from Reaction table)
        var commentCounts = await dbContext.Reaction
            .Where(r => r.TargetType == ReactionTargetType.Quote && quoteIds.Contains(r.TargetId))
            .GroupBy(r => r.TargetId)
            .Select(g => new
            {
                TargetId = g.Key,
                Count = g.Count() // In this new system, all reactions on quotes are considered comments
            })
            .ToDictionaryAsync(x => x.TargetId, x => x.Count);

        foreach (var dto in quoteDtos)
        {
            if (voteInfo.TryGetValue(dto.Id, out var vInfo))
            {
                dto.VoteCount = vInfo.Count;
                dto.HasVoted = vInfo.HasVoted;
            }

            if (commentCounts.TryGetValue(dto.Id, out var cCount))
            {
                dto.CommentsCount = cCount;
            }
        }

        return quoteDtos;
    }

    public async Task<QuoteDto?> GetQuoteByIdAsync(Guid id, string? userId = null)
    {
        await EnsureUserHasAccess(userId);

        var quote = await quoteRepository.GetById(id);
        if (quote == null) return null;

        // Check visibility
        if (quote.VisibleToRoles.Any())
        {
            if (userId == null) return null;
            var userRoles = await applicationUserService.GetUserRoles(userId);
            if (!userRoles.Contains(Roles.Admin) && !quote.VisibleToRoles.Any(r => userRoles.Contains(r.Name!)))
            {
                return null;
            }
        }

        var dto = MapToDto(quote);
        
        dto.VoteCount = await dbContext.QuoteVote.CountAsync(v => v.QuoteId == id);
        dto.HasVoted = userId != null && await dbContext.QuoteVote.AnyAsync(v => v.QuoteId == id && v.UserId == userId);
        dto.CommentsCount = await dbContext.Reaction.CountAsync(r => r.TargetType == ReactionTargetType.Quote && r.TargetId == id);

        return dto;
    }

    public async Task<bool> ToggleVoteAsync(Guid quoteId, string userId)
    {
        await EnsureUserHasAccess(userId);
        var existingVote = await dbContext.QuoteVote
            .FirstOrDefaultAsync(v => v.QuoteId == quoteId && v.UserId == userId);

        if (existingVote != null)
        {
            dbContext.QuoteVote.Remove(existingVote);
            await dbContext.SaveChangesAsync();
            return false;
        }

        var newVote = new QuoteVote
        {
            QuoteId = quoteId,
            UserId = userId
        };

        dbContext.QuoteVote.Add(newVote);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<QuoteDto> CreateQuoteAsync(QuoteCreateDto dto, string creatorId)
    {
        await EnsureUserHasAccess(creatorId);

        var quote = new Quote
        {
            Text = dto.Text,
            DateSaid = dto.DateSaid,
            CreatedById = creatorId,
            CreatedOn = DateTime.UtcNow
        };

        if (dto.VisibleToRoles != null && dto.VisibleToRoles.Any())
        {
            quote.VisibleToRoles = await dbContext.Roles
                .Where(r => dto.VisibleToRoles.Contains(r.Name!))
                .ToListAsync();
        }

        if (dto.Authors != null)
        {
            foreach (var authorDto in dto.Authors)
            {
                quote.Authors.Add(new QuoteAuthor
                {
                    ApplicationUserId = authorDto.ApplicationUserId,
                    CustomName = authorDto.CustomName
                });
            }
        }

        await quoteRepository.Add(quote);
        
        // Fetch again to include navigations for the mapping
        var createdQuote = await quoteRepository.GetById(quote.Id);

        if (dto.SendNotification && createdQuote != null)
        {
            await SendNewQuoteNotification(createdQuote, dto.VisibleToRoles);
        }

        return MapToDto(createdQuote!);
    }

    private async Task SendNewQuoteNotification(Quote quote, IEnumerable<string>? visibleToRoles)
    {
        var authorNames = quote.Authors.Select(a => a.CustomName ?? a.ApplicationUser?.FullName ?? "Iemand").ToList();
        var authorsText = authorNames.Count > 0 ? string.Join(" & ", authorNames) : "Iemand";
        
        var title = "Nieuwe Quote! 💬";
        var message = $"{authorsText}: \"{quote.Text}\"";
        var link = "/quotes";

        List<string>? targetUserIds = null;

        if (visibleToRoles != null && visibleToRoles.Any())
        {
            // Get users who have at least one of these roles
            targetUserIds = await dbContext.UserRoles
                .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => visibleToRoles.Contains(x.Name!))
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();
        }

        await notificationService.CreateCustomNotificationAsync(new CreateCustomNotificationDto
        {
            Title = title,
            Message = message,
            LinkUri = link,
            FanOut = targetUserIds == null, // If null, send to everyone
            UserIds = targetUserIds,
            SendPush = true
        });
    }

    public async Task<QuoteDto?> UpdateQuoteAsync(Guid id, QuoteUpdateDto dto, string userId)
    {
        await EnsureUserHasAccess(userId);

        var quote = await quoteRepository.GetById(id);
        if (quote == null) return null;

        await EnsureUserCanModify(quote, userId);

        quote.Text = dto.Text;
        quote.DateSaid = dto.DateSaid;

        // Update roles
        quote.VisibleToRoles.Clear();
        if (dto.VisibleToRoles != null && dto.VisibleToRoles.Any())
        {
            var newRoles = await dbContext.Roles
                .Where(r => dto.VisibleToRoles.Contains(r.Name!))
                .ToListAsync();
            foreach (var r in newRoles) quote.VisibleToRoles.Add(r);
        }

        // Update authors
        quote.Authors.Clear();
        if (dto.Authors != null)
        {
            foreach (var authorDto in dto.Authors)
            {
                quote.Authors.Add(new QuoteAuthor
                {
                    ApplicationUserId = authorDto.ApplicationUserId,
                    CustomName = authorDto.CustomName
                });
            }
        }

        await quoteRepository.Update(quote);
        return MapToDto(quote);
    }

    public async Task<bool> DeleteQuoteAsync(Guid id, string userId)
    {
        await EnsureUserHasAccess(userId);

        var quote = await quoteRepository.GetById(id);
        if (quote == null) return false;

        await EnsureUserCanModify(quote, userId);

        await quoteRepository.Delete(id);
        return true;
    }

    private async Task EnsureUserHasAccess(string? userId)
    {
        var setting = await dbContext.GlobalSetting.FirstOrDefaultAsync(s => s.Key == GlobalSettingKey);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            // If no setting is configured, we assume it's open for everyone or maybe just authenticated?
            // User request says "setting which decides the roles that are allowed to use this new Quotes feature"
            // So if it's missing, maybe it's not restricted yet.
            return;
        }

        if (userId == null) throw new UnauthorizedAccessException("Authentication required for Quotes feature.");

        var allowedRoles = setting.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var userRoles = await applicationUserService.GetUserRoles(userId);

        if (!userRoles.Contains(Roles.Admin) && !allowedRoles.Any(r => userRoles.Contains(r)))
        {
            throw new UnauthorizedAccessException("You do not have the required roles to use the Quotes feature.");
        }
    }

    private async Task EnsureUserCanModify(Quote quote, string userId)
    {
        var userRoles = await applicationUserService.GetUserRoles(userId);
        if (userRoles.Contains(Roles.Admin)) return;

        if (quote.CreatedById == userId) return;

        if (quote.Authors.Any(a => a.ApplicationUserId == userId)) return;

        throw new UnauthorizedAccessException("Only the creator, an admin, or an author of the quote can modify or delete it.");
    }

    private static QuoteDto MapToDto(Quote quote)
    {
        return new QuoteDto
        {
            Id = quote.Id,
            Text = quote.Text,
            DateSaid = quote.DateSaid,
            CreatedOn = quote.CreatedOn,
            CreatedBy = MapUserToDto(quote.CreatedBy),
            Authors = quote.Authors.Select(a => new QuoteAuthorDto
            {
                Id = a.Id,
                ApplicationUserId = a.ApplicationUserId,
                ApplicationUser = MapUserToDto(a.ApplicationUser),
                CustomName = a.CustomName
            }).ToList(),
            VisibleToRoles = quote.VisibleToRoles.Select(r => r.Name ?? string.Empty).ToList()
        };
    }

    private static ApplicationUserDto? MapUserToDto(ApplicationUser? user)
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
