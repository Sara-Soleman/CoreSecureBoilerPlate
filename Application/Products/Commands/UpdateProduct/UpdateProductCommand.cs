using Application.Common.Interfaces;
using Domain.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Commands.UpdateProduct
{
    public record UpdateProductCommand : IRequest<Unit>
    {
        public required Guid Id { get; init; }
        public required string NewName { get; init; }
        public required int NewStock { get; init; }
    }

    public class UpdateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateProductCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new BusinessRuleViolationException("The product to be modified does not exist.");

            // Update data via domain functions (which protect themselves from null names or negative inventory)

            // Note: Ensure that you have a function to update the name and inventory within your Product Entity
            product.UpdateDetails(request.NewName, request.NewStock);

            await productRepository.UpdateAsync(product, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
