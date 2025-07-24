using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MinimalApisVsControllers;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddDbContext<CustomerDbContext>(opt => opt.UseInMemoryDatabase("MyApp"));
services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);
services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Customers API", Version = "v1" });
});
services.AddEndpointsApiExplorer();
services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Customers API V1");
});
app.MapControllers();

//cool trick to apply filters to all endpoints
var root = app.MapGroup(string.Empty);
root.AddEndpointFilterFactory(ValidationFilter.ValidationFilterFactory); //see: https://benfoster.io/blog/minimal-api-validation-endpoint-filters/

//this uses route groups, introduced in .NET 7
root.MapCustomerEndpoints();

/*
 * GET /customers
 */
//NOTE: you have to declare the delegate separately to have default parameters
async Task<IResult> GetCustomers(CustomerDbContext db, int page = 1, int pageSize = 10)
{
    var customers = await db.Customers
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return Results.Ok(customers);
}

root.MapGet("/minimal/customers", GetCustomers)
    .WithTags("Customers via Minimal APIs")
    .WithMetadata(new SwaggerOperationAttribute("Retrieve customers by page"))
    .Produces(StatusCodes.Status200OK, responseType: typeof(IEnumerable<Customer>))
    .Produces(StatusCodes.Status500InternalServerError);

/*
 * GET /customers/{id}
 */
root.MapGet("/minimal/customers/{id}", async (int id, CustomerDbContext db) =>
    await db.Customers.FindAsync(id) is {} customer
        ? Results.Ok(customer)
        : Results.NotFound())
   .WithTags("Customers via Minimal APIs")
   .WithMetadata(new SwaggerOperationAttribute("Retrieve customer by ID"))
   .Produces(StatusCodes.Status200OK, responseType: typeof(Customer))
   .Produces(StatusCodes.Status404NotFound)
   .Produces(StatusCodes.Status500InternalServerError);

/*
 * POST /customers
 */
root.MapPost("/minimal/customers", async ([FromBody, Validate] CreateCustomerRequest request, CustomerDbContext db) =>
{
    var customer = new Customer { Name = request.Name };
    db.Customers.Add(customer);
    await db.SaveChangesAsync();
    return Results.Created($"/minimal/customers/{customer.Id}", customer);
})
   .WithTags("Customers via Minimal APIs")
   .WithMetadata(new SwaggerOperationAttribute("Creates a new customer"))
   .Produces(StatusCodes.Status201Created, responseType: typeof(Customer))
   .ProducesValidationProblem(StatusCodes.Status400BadRequest)
   .Produces(StatusCodes.Status500InternalServerError);

/*
 * PUT /customers/{id}
 */
root.MapPut("/minimal/customers/{id}", async (int id, [FromBody, Validate] UpdateCustomerRequest request, CustomerDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.Name = request.Name;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
   .WithTags("Customers via Minimal APIs")
   .WithMetadata(new SwaggerOperationAttribute("Updates a customer by ID"))
   .Produces(StatusCodes.Status204NoContent)
   .ProducesValidationProblem(StatusCodes.Status400BadRequest)
   .Produces(StatusCodes.Status404NotFound)
   .Produces(StatusCodes.Status500InternalServerError);

/*
 * DELETE /customers/{id}
 */
root.MapDelete("/minimal/customers/{id}", async (int id, CustomerDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    db.Customers.Remove(customer);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
   .WithTags("Customers via Minimal APIs")
   .WithMetadata(new SwaggerOperationAttribute("Deletes a customer by ID"))
   .Produces(StatusCodes.Status204NoContent)
   .Produces(StatusCodes.Status404NotFound)
   .Produces(StatusCodes.Status500InternalServerError);

if (args.Contains("--benchmark"))
{
    app.StartAsync();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    var customer = new Customer { Name = "test", Id = 1 };
    db.Add(customer);
    db.SaveChanges();
    
    BenchmarkRunner.Run<HttpBenchmark>(ManualConfig.Create(DefaultConfig.Instance)
        .WithOption(ConfigOptions.DisableLogFile, true)
        .WithOption(ConfigOptions.JoinSummary, true)
    );
}
else
{
    // Use the usual app.Run() if --benchmark is not in the arguments
    app.Run();
}