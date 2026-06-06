using Domain.Common;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Events
{
    public record ProductPriceChangedEvent(Guid ProductId, Money NewPrice) : IDomainEvent
    {
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
}
