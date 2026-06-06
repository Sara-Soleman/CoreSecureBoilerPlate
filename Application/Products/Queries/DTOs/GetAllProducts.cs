using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Queries.DTOs
{
    public record GetAllProducts() : IRequest<IReadOnlyList<ProductDto>>;



    public record GetAllProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

    public class GetAllProductsQueryHandler(IProductRepository productRepository)
        : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
    {
        public async Task<IReadOnlyList<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await productRepository.GetAllAsync(cancellationToken);

            
            return products.Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.Price.Amount,
                product.Price.Currency,
                product.StockQuantity
            )).ToList().AsReadOnly();
        }
    }

}
