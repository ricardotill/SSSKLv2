using MediatR;

namespace SSSKL.Application.Features.Users.Queries.GetPersonalData;

/// <summary>The user's properties marked with [PersonalData], for the GDPR download.</summary>
public sealed record GetPersonalDataQuery(string UserId) : IRequest<IReadOnlyDictionary<string, string>>;
