using PrimeNumberGenerator.EventArgs;
using System;
using System.IO;
using System.Text;

namespace PrimeNumberGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var generator = new PrimeGenerator();
            generator.OnLoadingPrimesFromResultFileStarted += Generator_OnLoadingPrimesFromResultFileStarted;
            generator.OnLoadingPrimesFromResultFileFinished += Generator_OnLoadingPrimesFromResultFileFinished;
            generator.OnPrimeGenerationStarted += Generator_OnPrimeGenerationStarted;
            generator.OnPrimesWrittenToFile += Generator_OnPrimesWrittenToFile;
            generator.OnLoadingPrimesFromFile += Generator_OnLoadingPrimesFromFile;

            Console.CursorVisible = false;
            Console.WriteLine("Press ESC to stop.");
            
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

        private static void Generator_OnLoadingPrimesFromResultFileStarted(object generator, LoadingPrimesFromResultFileStartedArgs args)
        {
            Console.WriteLine("Loading prime numbers from existing result files...");
        }

        private static void Generator_OnLoadingPrimesFromResultFileFinished(object generator, LoadingPrimesFromResultFileFinishedArgs args)
        {
            if (args.NumberOfResultFilesLoaded == 0)
            {
                Console.WriteLine("No existing result files were found. The generation will start from scratch.");
            }
            else
            {
                var format = "Prime number loading finished. {0} primes were loaded from {1} files.";
                var message = String.Format(format, args.NumberOfPrimesLoaded, args.NumberOfResultFilesLoaded);
                Console.WriteLine(message);
            }
        }

        private static void Generator_OnPrimeGenerationStarted(object generator, PrimeGenerationStartedArgs args)
        {
            Console.WriteLine("Generating prime numbers...");
        }

        private static void Generator_OnPrimesWrittenToFile(object generator, PrimesWrittenToFileArgs args)
        {
            var format = "{0}. Wrote primes #{1} to #{2} to file at {3} (generation time: {4} sec).";
            var message = String.Format(format, args.FileIndex, args.StartPrimeIndex + 1, args.EndPrimeIndex, args.WriteTime, args.GenerationDuration.TotalSeconds);
            Console.WriteLine(message);
        }

        private static void Generator_OnLoadingPrimesFromFile(object generator, PrimesLoadingFromFile args)
        {
            if (args.IndexOfFileToBeLoaded == 0)
            {
                Console.WriteLine();
            }

            var format = "Loading file {0} of {1}...";
            var message = String.Format(format, args.IndexOfFileToBeLoaded + 1, args.TotalAmountOfFiles);
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine(message);
        }
    }
}
