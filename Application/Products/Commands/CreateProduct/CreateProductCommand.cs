using Application.Common.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Commands.CreateProduct
{
    public record CreateProductCommand : IRequest<Guid>
    {
        public required string Name { get; init; }
        public required decimal PriceAmount { get; init; }
        public string Currency { get; init; } = "USD";
        public required int InitialStock { get; init; }
    }


    public class CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateProductCommand, Guid>
    {
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Create the value object for money (validation for negative price happens here automatically)
            var price = new Money(request.PriceAmount, request.Currency);

            // Create the product object (Entity) and generate a unique identifier (Guid) for it
            var productId = Guid.NewGuid();
            var product = new Product(productId, request.Name, price, request.InitialStock);

            // Add the product to the repository in memory
            await productRepository.AddAsync(product, cancellationToken);

            // Save changes permanently to the database
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Return the ID of the newly created product to the API
            return product.Id;
        }
    }
}
