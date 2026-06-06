using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Products.Commands.CreateProduct
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Product name required.")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

            RuleFor(v => v.PriceAmount)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");
        }
    }
}
