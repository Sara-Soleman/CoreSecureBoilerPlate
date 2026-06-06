using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions
{
    public abstract class DomainException(string message) : Exception(message);

    public class InsufficientFundsException(decimal requested, decimal available)
        : DomainException($"Sorry, insufficient funds. Requested amount: {requested}, Currently available: {available}.");

    public class NegativePriceException()
        : DomainException("Product price cannot be negative or zero.");
    public class InvalidQuantityException()
        : DomainException("Quantity to deduct must be greater than zero.");

    // Custom error for out of stock
    public class OutOfStockException(string productName, int availableStock)
        : DomainException($"Insufficient stock for product {productName}. Currently available: {availableStock}");

    public class InvalidProductNameException()
    : DomainException("Product name cannot be empty.");

    public class InvalidInitialStockException()
        : DomainException("Initial stock cannot be negative.");

    public class CurrencyMismatchException(string expected, string actual)
    : DomainException($"Cannot perform calculations on different currencies! Expected currency: {expected}, Received currency: {actual}.");

}