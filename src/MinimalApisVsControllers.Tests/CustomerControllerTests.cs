using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;

namespace MinimalApisVsControllers.Tests;

public class CustomerControllerTests : CustomerTests
{
    public CustomerControllerTests() : base("/controller")
    {
    }
}