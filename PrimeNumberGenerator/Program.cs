using System;

namespace PrimeNumberGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var generator = new PrimeGenerator(10000);
            generator.OnPrimesWrittenToFile += Generator_OnPrimesWrittenToFile;

            Console.WriteLine("Press ESC to stop.");
            Console.WriteLine("Generating prime numbers...");
            generator.GeneratePrimes();
        }

        private static void Generator_OnPrimesWrittenToFile(object generator, PrimesWrittenToFileArgs args)
        {
            var format = "{0}. Wrote primes #{1} to #{2} to file at {3} (generation time: {4} sec).";
            var message = String.Format(format, args.FileIndex, args.StartPrimeIndex + 1, args.EndPrimeIndex + 1, args.WriteTime, args.GenerationDuration.TotalSeconds);
            Console.WriteLine(message);
        }
    }
}
