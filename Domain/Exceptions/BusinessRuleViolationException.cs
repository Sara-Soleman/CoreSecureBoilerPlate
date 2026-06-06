using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions
{
    public class BusinessRuleViolationException(string message) : DomainException(message);
}
