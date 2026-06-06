using Domain.Entities;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Services
{
    public class PricingDomainService
    {
        
        public Money CalculateSpecialDiscount(Product product, bool isVipCustomer)
        {
            if (isVipCustomer && product.StockQuantity > 10)
            {
               
                decimal discountedAmount = product.Price.Amount * 0.90m;
                return new Money(discountedAmount, product.Price.Currency);
            }

            return product.Price;
        }
    }
}
