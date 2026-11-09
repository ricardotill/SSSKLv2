using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.TopUps.Commands.CreateTopUp;

public sealed record CreateTopUpCommand(string UserName, decimal Saldo) : IRequest<TopUpDto>;
