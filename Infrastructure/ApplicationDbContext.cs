using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;


///dotnet ef migrations add InitialCreate --project Infrastructure --startup-project CoreSecureBoilerPlate
///dotnet ef database update --project Infrastructure --startup-project CoreSecureBoilerPlate
namespace Infrastructure
{
    /*
    * This class not only saves data, but also catches domain events immediately before saving, allowing the application layer or 
    * event system to publish them later.
    */
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher)
     : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
    {



        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatically apply all configurations found in the Assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

           
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //Capture all entities containing pending domain events
            var domainEntities = ChangeTracker.Entries<BaseEntity<Guid>>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            var domainEvents = domainEntities.SelectMany(x => x.DomainEvents).ToList();

            //Clear events from entities to prevent duplication
            domainEntities.ForEach(x => x.ClearDomainEvents());

            // Actual data saving in the database
            var result = await base.SaveChangesAsync(cancellationToken);

            //Post the events after successful saving so that other Handlers can interact with them (e.g., sending an email).
            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }

            return result;
        }
    }
}
