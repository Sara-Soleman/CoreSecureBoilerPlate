using Application.Common.Interfaces;
using Domain.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Queries.DTOs
{
   
    //record for Immutable Data
    public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
    //this is positional Record for this
    /*
     * public record GetProductByIdQuery
     *  {
     *      public Guid Id { get; init; }
     *      public GetProductByIdQuery(Guid id) => Id = id;
     *  }
     */

   
    public class GetProductByIdQueryHandler(IProductRepository productRepository)
        : IRequestHandler<GetProductByIdQuery, ProductDto>
    {
        public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await productRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new BusinessRuleViolationException($"Product with ID {request.Id} does not exist.");


            return new ProductDto(
                product.Id,
                product.Name,
                product.Price.Amount,
                product.Price.Currency,
                product.StockQuantity
            );
        }
    }


}
