using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace MinimalApisVsControllers;

[ApiController]
[Route("controller/[controller]")]
[Produces("application/json")]
[SwaggerTag("Customers via Controllers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _db;

    public CustomersController(CustomerDbContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Customer>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(Summary = "Retrieve customers by page")]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll(int page = 1, int pageSize = 10)
    {
        return await _db.Customers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Customer))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Retrieve customer by ID")]
    public async Task<ActionResult<Customer>> Get(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return customer;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Customer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(Summary = "Creates a new customer")]
    public async Task<ActionResult<Customer>> Post([FromBody, Validate] CreateCustomerRequest request)
    {
        var customer = new Customer { Name = request.Name };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(Summary = "Updates a customer by ID")]
    public async Task<IActionResult> Put(int id, [FromBody, Validate] UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        customer.Name = request.Name;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(Summary = "Deletes a customer by ID")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
