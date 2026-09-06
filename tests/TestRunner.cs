using System;
using System.Collections.Generic;

namespace Abyss.Portfolio.Tests
{
    /// <summary>외부 테스트 프레임워크 없이 샘플 계약을 실행하는 작은 콘솔 러너입니다.</summary>
    internal sealed class TestRunner
    {
        private int passed;
        private int failed;
        private int assertions;

        public void Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine("FAIL " + name + ": " + exception.Message);
            }
        }

        public void Equal<T>(T expected, T actual, string message)
        {
            assertions++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " | expected=" + expected + ", actual=" + actual);
        }

        public void True(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }

        public void Throws<TException>(Action action, string message) where TException : Exception
        {
            assertions++;
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        public int Finish()
        {
            Console.WriteLine("\n" + passed + " tests passed; " + failed + " failed; " + assertions + " assertions.");
            return failed == 0 ? 0 : 1;
        }
    }
}
