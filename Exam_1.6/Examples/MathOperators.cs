public static class MathOps
{
    public static int Abs(int x)
    {
        return x >= 0 ? x : -x;
    }

    public static int Factorial(int n)
    {
        int result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }
}