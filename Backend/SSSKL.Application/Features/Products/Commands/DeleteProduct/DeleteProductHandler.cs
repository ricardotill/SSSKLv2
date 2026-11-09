using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Products.Commands.DeleteProduct;

internal sealed class DeleteProductHandler(IProductRepository productRepository)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // Throws NotFoundException for an unknown id, which the API turns into a 404.
        await productRepository.GetById(request.Id);
        await productRepository.Delete(request.Id);
    }
}
