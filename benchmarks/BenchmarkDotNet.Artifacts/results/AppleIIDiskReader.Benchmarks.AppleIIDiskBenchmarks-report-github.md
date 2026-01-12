```

BenchmarkDotNet v0.14.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 9.0.10 (9.0.1025.47515), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 9.0.10 (9.0.1025.47515), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------------- |---------:|---------:|---------:|-------:|----------:|
| &#39;Read VTOC properties&#39; | 50.42 ns | 30.85 ns | 1.691 ns | 0.0430 |     360 B |
