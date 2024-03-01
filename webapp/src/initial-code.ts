export const INITIAL_CODE = `// visit https://benchmarkdotnet.org/ for more info on BenchmarkDotNet

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

[MemoryDiagnoser]
public class Benchmarks
{
    // Write your benchmarks here
    const int size = 1_000_000_000;
    private int[] array = new int[size];

    public Benchmarks()
    {
      for (int i = 0; i < size; i++)
      {
        array[i] = i + 1;
      }
    }

    [Benchmark]
    public int ArraySum()
    {
        int sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }

        return sum;
    }
}
`