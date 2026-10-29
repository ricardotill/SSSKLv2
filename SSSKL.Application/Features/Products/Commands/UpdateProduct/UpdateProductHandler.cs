using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Products.Commands.UpdateProduct;

internal sealed class UpdateProductHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Product;
        var existing = await productRepository.GetById(request.Id);

        existing.Name = dto.Name;
        existing.Description = dto.Description ?? string.Empty;
        existing.Price = dto.Price;
        existing.Stock = dto.Stock;
        existing.EnableLeaderboard = dto.EnableLeaderboard;

        await productRepository.Update(existing);
    }
}
