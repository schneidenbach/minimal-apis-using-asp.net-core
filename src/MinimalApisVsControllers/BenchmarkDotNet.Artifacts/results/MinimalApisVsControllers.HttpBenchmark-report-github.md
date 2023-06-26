``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Monterey 12.5 (21G72) [Darwin 21.6.0]
Apple M1 Pro, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 7.0.4 (7.0.423.11508), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.4 (7.0.423.11508), Arm64 RyuJIT AdvSIMD


```
|                        Method |     Mean |    Error |   StdDev |   Median | Allocated |
|------------------------------ |---------:|---------:|---------:|---------:|----------:|
|            GetCustomerMinimal | 407.2 μs | 17.11 μs | 47.12 μs | 390.0 μs |   3.15 KB |
| GetCustomerMinimalMethodGroup | 368.5 μs |  7.31 μs |  7.18 μs | 365.6 μs |   3.14 KB |
|         GetCustomerController | 365.0 μs |  7.30 μs |  9.74 μs | 361.6 μs |   3.15 KB |
