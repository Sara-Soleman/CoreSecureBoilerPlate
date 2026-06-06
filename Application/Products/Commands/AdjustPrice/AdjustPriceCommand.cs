using Application.Common.Interfaces;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Commands.AdjustPrice
{
    //We use the required feature to ensure that no null values ​​are sent from the API.
    public record AdjustPriceCommand : IRequest<Unit>
    {
        public required Guid ProductId { get; init; }
        public required decimal NewAmount { get; init; }
        public string Currency { get; init; } = "USD";
    }

    //The processor injects the Repository and UnitOfWork together via the Primary Constructor.
    public class AdjustPriceCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<AdjustPriceCommand, Unit>
    {
        public async Task<Unit> Handle(AdjustPriceCommand request, CancellationToken cancellationToken)
        {
            // Fetch the object from the database via the repository
            var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
                ?? throw new BusinessRuleViolationException("The product to be updated does not exist.");

            // Create the value object (Value Object) safely (validation happens inside it automatically)
            var newPrice = new Money(request.NewAmount, request.Currency);

            // Call business rules and change the state inside the entity itself (Rich Domain Model)
            // This method will automatically raise the Domain Event as well
            product.AdjustPrice(newPrice);

            // 4. Update the repository and save changes via the Unit of Work
            await productRepository.UpdateAsync(product, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
