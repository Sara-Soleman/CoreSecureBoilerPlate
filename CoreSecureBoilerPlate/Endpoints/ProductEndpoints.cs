using Application.Products.Commands.AdjustPrice;
using Application.Products.Commands.CreateProduct;
using Application.Products.Commands.DeleteProduct;
using Application.Products.Commands.UpdateProduct;
using Application.Products.Queries.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;

namespace CoreSecureBoilerPlate.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            // Create a group to organize links and add a unified prefix
            var group = app.MapGroup("/api/products")
                .WithTags("Products"); 

            
            group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetProductByIdQuery(id);//query in CQRS
                var result = await sender.Send(query, cancellationToken);// sent the request to the handler to get the data
                return Results.Ok(result);
            })
            .WithName("GetProductById")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();//Only a valid token is required

            
            group.MapPut("/adjust-price", async (AdjustPriceCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                // MediatR will pass the command to the handler, which applies the domain rules and saves in EF Core
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("AdjustPrice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            //.RequireAuthorization(new AuthorizationPolicyBuilder()
            //.RequireRole("Admin").Build()) //
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting("ProductPolicy");


           
            group.MapPost("/", async (CreateProductCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var productId = await sender.Send(command, cancellationToken);
                return Results.CreatedAtRoute("GetProductById", new { id = productId }, productId);
            })
            .WithName("CreateProduct")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);


            group.MapGet("/GetAllProducts", async ( ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetAllProductsQuery();
                var results = await sender.Send(query, cancellationToken);
                return Results.Ok(results);

            })
            .WithName("GetAllProducts")
            .Produces<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)
            .RequireRateLimiting("LoginPolicy");


           
            group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                if (id != command.Id) return Results.BadRequest("Product ID not matching.");

                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("UpdateProduct")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

          
            group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new DeleteProductCommand(id);
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteProduct")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);



        }
    }
}
