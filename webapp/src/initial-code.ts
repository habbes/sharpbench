export const INITIAL_CODE = `// visit https://benchmarkdotnet.org/ for more info on BenchmarkDotNet

using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

[CategoriesColumn]
public class Benchmarks
{
    private int[]? A;
    private int[]? B;
    private int[]? C;

    public int dataSize = 0x1000;

    [GlobalSetup]
    public void Setup()
    {
        A = new int[dataSize];
        B = new int[dataSize];
        C = new int[dataSize];

        Random generator = new Random(1);
        for (int i = 0; i < dataSize; i++)
        {
            A[i] = generator.Next(0, 10);
            B[i] = generator.Next(0, 10);
            C[i] = 0;
        }
    }


    [BenchmarkCategory("MemberWiseSum"), Benchmark]
    public void MemberWiseSumScalar() => Compute.MemberWiseSumScalar(A, B, C);

    [BenchmarkCategory("MemberWiseSum"), Benchmark]
    public void MemberWiseSumSIMD() => Compute.MemberWiseSumSIMD(A, B, C);

    [BenchmarkCategory("ArraySum"), Benchmark]
    public void ArraySumSalar() => Compute.ArraySumScalar(A);


    [BenchmarkCategory("ArraySum"), Benchmark]
    public void ArraySumSIMD() => Compute.ArraySumSIMD(A);

}
public static class Compute
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemberWiseSumScalar(int[] A, int[] B, int[] result)
    {
        int size = A.Length;
        
        for (int i = 0; i < size; i++)
        {
            result[i] = A[i] + B[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemberWiseSumSIMD(int[] A, int[] B, int[] result)
    {
        int size = A.Length;

        for (int i = 0; i < size; i += Vector<int>.Count)
        {
            Vector<int> v = new Vector<int>(A, i) + new Vector<int>(B, i);
            v.CopyTo(result, i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ArraySumScalar(int[] A)
    {
        int sum = 0;
        foreach(int value in A)
        {
            sum += value;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ArraySumSIMD(int[] A)
    {
        Vector<int> sums = Vector<int>.Zero;

        for (int i = 0; i < A.Length; i += Vector<int>.Count)
        {
            sums += new Vector<int>(A, i);
        }

        int finalSum = 0;
        for (int n = 0; n < Vector<int>.Count; n++)
        {
            finalSum += sums[n];
        }

        return finalSum;
    }
}
`