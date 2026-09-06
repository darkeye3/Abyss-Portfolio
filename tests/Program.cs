using System;

namespace Abyss.Portfolio.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            Console.WriteLine("Abyss | Selected C# samples\n");
            TestRunner runner = new TestRunner();
            RandomnessTests.Register(runner);
            GraphSearchTests.Register(runner);
            return runner.Finish();
        }
    }
}
