using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Commands.MarkUnseenEntriesSeen;

/// <summary>Returns the user's unseen achievement entries and marks them as seen.</summary>
public sealed record MarkUnseenEntriesSeenCommand(string Username) : IRequest<IReadOnlyList<AchievementEntryDto>>;
