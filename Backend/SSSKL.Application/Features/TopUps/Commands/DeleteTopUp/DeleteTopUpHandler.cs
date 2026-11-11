using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.TopUps.Commands.DeleteTopUp;

internal sealed class DeleteTopUpHandler(ITopUpRepository topUpRepository, IUserStatRepository userStatRepository)
    : IRequestHandler<DeleteTopUpCommand>
{
    public async Task Handle(DeleteTopUpCommand request, CancellationToken cancellationToken)
    {
        var topUp = await topUpRepository.GetById(request.Id);
        await topUpRepository.Delete(request.Id);
        await userStatRepository.RecalculateByUserId(topUp.User.Id);
    }
}
