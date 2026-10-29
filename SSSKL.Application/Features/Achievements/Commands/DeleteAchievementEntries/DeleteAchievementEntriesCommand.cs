using MediatR;

namespace SSSKL.Application.Features.Achievements.Commands.DeleteAchievementEntries;

public sealed record DeleteAchievementEntriesCommand(IReadOnlyList<Guid> EntryIds) : IRequest;
