using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Product : BaseEntity<Guid>
    {
        
        public string Name { get; private set; } = default!;
        public Money Price { get; private set; } = default!;
        public int StockQuantity { get; private set; }

       
        private Product() { }

        public Product(Guid id, string name, Money price, int initialStock)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidProductNameException();

            if (initialStock < 0)
                throw new InvalidInitialStockException();

            Id = id;
            Name = name;
            Price = price;
            StockQuantity = initialStock;
        }


        public void AdjustPrice(Money newPrice)
        {
            ArgumentNullException.ThrowIfNull(newPrice);

            if (Price == newPrice) return; // لم يتغير شيء

            Price = newPrice;

            RaiseDomainEvent(new ProductPriceChangedEvent(Id, Price));
        }


        public void DeductStock(int quantity)
        {
            if (quantity <= 0)
                throw new InvalidQuantityException(); 

            if (StockQuantity < quantity)
                throw new OutOfStockException(Name, StockQuantity); 
            //here we can add domain event to tell the system there is no products in the stock

            StockQuantity -= quantity;
        }

        public void UpdateDetails(string newName, int newStock)
        {
            
            if (string.IsNullOrWhiteSpace(newName))
                throw new BusinessRuleViolationException("Product name cannot be empty.");

            if (newStock < 0)
                throw new BusinessRuleViolationException("Stock quantity cannot be negative.");

            Name = newName;
            StockQuantity = newStock;
        }
    }
}
