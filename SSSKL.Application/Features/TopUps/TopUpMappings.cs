using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.TopUps;

public static class TopUpMappings
{
    public static TopUpDto ToDto(this TopUp t) => new()
    {
        Id = t.Id,
        UserName = t.User?.UserName,
        Saldo = t.Saldo,
        CreatedOn = t.CreatedOn
    };
}
