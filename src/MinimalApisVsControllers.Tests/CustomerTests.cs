using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApisVsControllers.Tests;

public abstract class CustomerTests : IDisposable
{
    private readonly string _basePartOfUrl;
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _application;

    protected CustomerTests(string basePartOfUrl)
    {
        _basePartOfUrl = basePartOfUrl;
        _application = new WebApplicationFactory<Program>();
        _client = _application.CreateClient();
    }

    [Fact]
    public async Task Get_Customers_ReturnsOk()
    {
        AddCustomer("alltest");
        AddCustomer("allmeow");
        
        //Act
        var response = await _client.GetAsync($"{_basePartOfUrl}/customers");

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
        
        var customers = await response.Content.ReadFromJsonAsync<List<Customer>>();
        Assert.Contains(customers, customer => customer.Name == "allmeow");
        Assert.Contains(customers, customer => customer.Name == "alltest");
    }

    [Fact]
    public async Task Get_Customer_ReturnsOk()
    {
        //Arrange
        var newCustomer = AddCustomer("test");
        
        //Act
        var response = await _client.GetAsync($"{_basePartOfUrl}/customers/" + newCustomer.Id);

        //Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Update_Customer_ReturnsOk()
    {
        //Arrange
        var newCustomer = AddCustomer("test");

        //Act
        var response = await _client.PutAsJsonAsync($"{_basePartOfUrl}/customers/{newCustomer.Id}", 
            new { Name = "meow" });

        //Assert
        response.EnsureSuccessStatusCode();
        
        var customer = GetDb().Customers.Single(c => c.Id == newCustomer.Id);
        
        Assert.Equal("meow", customer.Name);
    }

    [Fact]
    public async Task Update_Customer_ReturnsBadRequest()
    {
        //Arrange
        var newCustomer = AddCustomer("test");

        //Act
        var response = await _client.PutAsJsonAsync($"{_basePartOfUrl}/customers/{newCustomer.Id}", 
            new { Name = string.Empty });

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Customer_ReturnsNotFound()
    {
        //Act
        var response = await _client.PutAsJsonAsync($"{_basePartOfUrl}/customers/999", 
            new { Name = "meow" });

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Customer_ReturnsOk()
    {
        //Act
        var response = await _client.PostAsJsonAsync($"{_basePartOfUrl}/customers", 
            new Customer { Name = "Test" });

        //Assert
        response.EnsureSuccessStatusCode();
        var newCustomer = await response.Content.ReadFromJsonAsync<Customer>();
        
        var customer = GetDb().Customers.Single(c => c.Id == newCustomer!.Id);
        Assert.Equal("Test", customer.Name);
    }

    [Fact]
    public async Task Create_Customer_ReturnsBadRequest()
    {
        //Act
        var response = await _client.PostAsJsonAsync($"{_basePartOfUrl}/customers", 
            new Customer { Name = string.Empty });

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Customer_ReturnsOk()
    {
        //Arrange
        var newCustomer = AddCustomer("test");

        //Act
        var response = await _client.DeleteAsync($"{_basePartOfUrl}/customers/{newCustomer.Id}");

        //Assert
        response.EnsureSuccessStatusCode();
        
        var db = GetDb();
        Assert.Empty(db.Customers.Where(c => c.Id == newCustomer.Id));
    }

    private Customer AddCustomer(string name)
    {
        using var scope = _application.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer { Name = name };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer;
    }

    private CustomerDbContext GetDb()
    {
        var scope = _application.Server.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    }

    public void Dispose()
    {
        _application.Dispose();
        _client.Dispose();
    }
}