using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Queries.GetUsers;

/// <param name="ForAdmin">True includes every account (the admin user list); false only the regular user list.</param>
public sealed record GetUsersQuery(int Skip, int Take, bool ForAdmin = false) : IRequest<PaginationObject<ApplicationUserDto>>;
