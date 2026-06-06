using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.ValueObjects
{
    public record Money
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; }

       
        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0) throw new NegativePriceException();
            if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency cannot be empty.");

            Amount = amount;
            Currency = currency.ToUpper();
        }

        
        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new CurrencyMismatchException(expected: Currency, actual: other.Currency);
            //throw new BusinessRuleViolationException("You Can only add amounts in the same currency.");

            return new Money(Amount + other.Amount, Currency);
        }
    }
}
