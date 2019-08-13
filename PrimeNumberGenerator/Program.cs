﻿using System;
using System.IO;
using System.Text;

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

            try
            {
                generator.GeneratePrimes();
            }
            catch (Exception ex)
            {
                writeErrorLogFile(ex);
            }
        }

        private static void writeErrorLogFile(Exception ex)
        {
            using (var stream = new StreamWriter("GeneratorFailureLog.txt"))
            {
                var message = new StringBuilder();
                message.AppendFormat("Time: {0}", DateTime.Now);
                message.AppendLine();
                message.AppendFormat("Error: {0}", ex.GetType().Name);
                message.AppendLine();
                message.AppendFormat("Message: {0}", ex.Message);
                message.AppendLine();
                if (ex.Data.Contains("CurrentNumberToCheck"))
                {
                    message.AppendFormat("Current number to check: {0}", ex.Data["CurrentNumberToCheck"]);
                    message.AppendLine();
                }
                message.AppendFormat("Source: {0}", ex.Source);
                message.AppendLine();
                message.AppendFormat("Stack Trace:\n{0}", ex.StackTrace);
                
                stream.WriteLine(message.ToString());
            }
        }

        private static void Generator_OnPrimesWrittenToFile(object generator, PrimesWrittenToFileArgs args)
        {
            var format = "{0}. Wrote primes #{1} to #{2} to file at {3} (generation time: {4} sec).";
            var message = String.Format(format, args.FileIndex, args.StartPrimeIndex + 1, args.EndPrimeIndex + 1, args.WriteTime, args.GenerationDuration.TotalSeconds);
            Console.WriteLine(message);
        }
    }
}
