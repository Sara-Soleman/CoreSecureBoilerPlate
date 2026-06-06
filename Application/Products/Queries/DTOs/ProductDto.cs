using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Queries.DTOs
{

    public record ProductDto(Guid Id, string Name, decimal Price, string Currency, int StockQuantity);
}
