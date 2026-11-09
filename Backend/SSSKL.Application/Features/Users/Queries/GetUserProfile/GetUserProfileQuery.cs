using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(string Id) : IRequest<ApplicationUserDto>;
