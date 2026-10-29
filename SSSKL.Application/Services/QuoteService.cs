using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;


namespace SSSKL.Application.Services;

public class QuoteService(
    IQuoteRepository quoteRepository,
    IApplicationUserService applicationUserService,
    IReactionRepository reactionRepository,
    IRoleRepository roleRepository,
    IGlobalSettingRepository globalSettingRepository,
    INotificationService notificationService,
    IDomainEventDispatcher eventDispatcher,
    IUserStatRepository userStatRepository) : IQuoteService
{
    private const string GlobalSettingKey = "QuotesFeatureAllowedRoles";

    public async Task<PaginationObject<QuoteDto>> GetQuotesAsync(int skip = 0, int take = 15, string? userId = null, string? targetUserId = null)
    {
        await EnsureUserHasAccess(userId);

        IList<string>? userRoles = null;
        bool isAdmin = false;
        if (userId != null)
        {
            userRoles = await applicationUserService.GetUserRoles(userId);
            isAdmin = userRoles.Contains(Roles.Admin);
        }

        var (quotes, totalCount) = await quoteRepository.GetAll(skip, take, userRoles, isAdmin, targetUserId);
        var quoteDtos = quotes.Select(MapToDto).ToList();

        var quoteIds = quoteDtos.Select(q => q.Id).ToList();
        
        // Fetch Vote counts and user's voted state
        var voteInfo = await quoteRepository.GetVoteSummaries(quoteIds, userId);

        // Fetch Comment counts (from Reaction table); all reactions on quotes are considered comments
        var commentCounts = await reactionRepository.CountByTargets(ReactionTargetType.Quote, quoteIds);

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

        return new PaginationObject<QuoteDto>
        {
            Items = quoteDtos,
            TotalCount = totalCount
        };
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
        
        var voteInfo = await quoteRepository.GetVoteSummaries([id], userId);
        if (voteInfo.TryGetValue(id, out var vInfo))
        {
            dto.VoteCount = vInfo.Count;
            dto.HasVoted = vInfo.HasVoted;
        }
        dto.CommentsCount = await reactionRepository.CountByTarget(ReactionTargetType.Quote, id);

        return dto;
    }

    public async Task<bool> ToggleVoteAsync(Guid quoteId, string userId)
    {
        await EnsureUserHasAccess(userId);
        var existingVote = await quoteRepository.GetVote(quoteId, userId);

        if (existingVote != null)
        {
            await quoteRepository.DeleteVote(existingVote);

            var affectedUserIds = await quoteRepository.GetAuthorUserIds(quoteId);
            if (!affectedUserIds.Contains(userId))
            {
                affectedUserIds.Add(userId);
            }

            foreach (var affectedUserId in affectedUserIds)
            {
                await userStatRepository.RecalculateByUserId(affectedUserId);
            }
            return false;
        }

        var newVote = new QuoteVote
        {
            QuoteId = quoteId,
            UserId = userId
        };

        await quoteRepository.AddVote(newVote);
        await eventDispatcher.DispatchAsync(new QuoteVotedEvent(newVote));
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
            quote.VisibleToRoles = await roleRepository.GetByNames(dto.VisibleToRoles);
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
        await eventDispatcher.DispatchAsync(new QuoteCreatedEvent(quote));
        
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
            targetUserIds = (await roleRepository.GetUserIdsInRoles(visibleToRoles)).ToList();
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
            var newRoles = await roleRepository.GetByNames(dto.VisibleToRoles);
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

        var affectedUserIds = await quoteRepository.GetAuthorUserIds(id);
        if (!affectedUserIds.Contains(quote.CreatedById))
        {
            affectedUserIds.Add(quote.CreatedById);
        }

        await quoteRepository.Delete(id);
        foreach (var affectedUserId in affectedUserIds)
        {
            await userStatRepository.RecalculateByUserId(affectedUserId);
        }
        return true;
    }

    private async Task EnsureUserHasAccess(string? userId)
    {
        var setting = await globalSettingRepository.GetByKey(GlobalSettingKey);
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
