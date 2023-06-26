using BenchmarkDotNet.Attributes;

namespace MinimalApisVsControllers;


[MemoryDiagnoser]
public class HttpBenchmark
{
    private readonly HttpClient _client = new HttpClient();

    [Benchmark]
    public async Task GetCustomerMinimal()
    {
        await _client.GetAsync($"http://localhost:5071/minimal/customers/1");
    }
    
    [Benchmark]
    public async Task GetCustomerMinimalMethodGroup()
    {
        await _client.GetAsync($"http://localhost:5071/minimal/customers/1");
    }
    
    [Benchmark]
    public async Task GetCustomerController()
    {
        await _client.GetAsync($"http://localhost:5071/minimal/customers/1");
    }
}
