using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace MinimalApisVsControllers;

public static class CustomerApiGroup
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("/minimalgroup/customers")
            .WithTags("Customers via Minimal API Groups");

        /*
         * GET /customers
         */
        //NOTE: you have to declare the delegate separately to have default parameters
        async Task<Ok<List<Customer>>> GetCustomers(CustomerDbContext db, int page = 1, int pageSize = 10)
        {
            var customers = await db.Customers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return TypedResults.Ok(customers);
        }

        group.MapGet(string.Empty, GetCustomers)
            .WithMetadata(new SwaggerOperationAttribute("Retrieve customers by page"))
            .Produces(StatusCodes.Status500InternalServerError);

        /*
         * GET /customers/{id}
         */
        group.MapGet("{id}", async Task<Results<Ok<Customer>, NotFound>> (int id, CustomerDbContext db) =>
                await db.Customers.FindAsync(id) is { } customer
                    ? TypedResults.Ok(customer)
                    : TypedResults.NotFound())
            .WithMetadata(new SwaggerOperationAttribute("Retrieve customer by ID"))
            .Produces(StatusCodes.Status500InternalServerError);

        /*
         * POST /customers
         */
        group.MapPost("",
                async Task<Created<Customer>> ([FromBody, Validate] CreateCustomerRequest request, CustomerDbContext db) =>
                {
                    var customer = new Customer { Name = request.Name };
                    db.Customers.Add(customer);
                    await db.SaveChangesAsync();
                    return TypedResults.Created($"/minimal/customers/{customer.Id}", customer);
                })
            .WithMetadata(new SwaggerOperationAttribute("Creates a new customer"))
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        /*
         * PUT /customers/{id}
         */
        group.MapPut("{id}",
                async Task<Results<NoContent, NotFound>> (int id, [FromBody, Validate] UpdateCustomerRequest request, CustomerDbContext db) =>
                {
                    var customer = await db.Customers.FindAsync(id);
                    if (customer == null)
                    {
                        return TypedResults.NotFound();
                    }

                    customer.Name = request.Name;
                    await db.SaveChangesAsync();
                    return TypedResults.NoContent();
                })
            .WithMetadata(new SwaggerOperationAttribute("Updates a customer by ID"))
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        /*
         * DELETE /customers/{id}
         */
        group.MapDelete("{id}", async Task<Results<NoContent, NotFound>> (int id, CustomerDbContext db) =>
            {
                var customer = await db.Customers.FindAsync(id);
                if (customer == null)
                {
                    return TypedResults.NotFound();
                }

                db.Customers.Remove(customer);
                await db.SaveChangesAsync();
                return TypedResults.NoContent();
            })
            .WithMetadata(new SwaggerOperationAttribute("Deletes a customer by ID"))
            .Produces(StatusCodes.Status500InternalServerError);
    }
}