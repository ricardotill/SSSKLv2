using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Queries.GetUserDetails;

public sealed record GetUserDetailsQuery(UserReference User) : IRequest<ApplicationUserDetailedDto>;
