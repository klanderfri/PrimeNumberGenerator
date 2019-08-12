using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace PrimeNumberGenerator
{
    /// <summary>
    /// Class for object generating prime numbers.
    /// </summary>
    public class PrimeGenerator
    {
        /// <summary>
        /// The amount of primes to put in each result-file.
        /// </summary>
        private readonly int NumberOfPrimesInFile;
        
        /// <summary>
        /// The index of the next result-file.
        /// </summary>
        private int NextFileIndex;

        /// <summary>
        /// The point in time when the last result-file was written.
        /// </summary>
        private DateTime LastFileWrite;

        /// <summary>
        /// All found primes.
        /// </summary>
        public List<BigInteger> Primes { get; private set; }

        /// <summary>
        /// Handler for the OnPrimesWrittenToFile-event.
        /// </summary>
        /// <param name="generator">The generator causing the result-file to be written.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void PrimesWrittenToFileHandler(object generator, PrimesWrittenToFileArgs args);

        /// <summary>
        /// Event raised when primes are written to a result-file.
        /// </summary>
        public event PrimesWrittenToFileHandler OnPrimesWrittenToFile;

        /// <summary>
        /// Creates an object generating prime numbers.
        /// </summary>
        public PrimeGenerator(int numberOfPrimesInFile)
        {
            Primes = new List<BigInteger>(1048576);
            NextFileIndex = 1;
            NumberOfPrimesInFile = numberOfPrimesInFile;
        }

        /// <summary>
        /// Starts the prime number generation.
        /// </summary>
        public void GeneratePrimes()
        {
            //Reset the generation.
            Primes.Clear();
            LastFileWrite = DateTime.Now;
            var lastHeartbeat = DateTime.Now;
            var numberToCheck = new BigInteger(2);

            do
            {
                while (!Console.KeyAvailable)
                {
                    //Check if the number is a prime.
                    bool isPrime = isPrimeNumber(Primes, numberToCheck);

                    //Handle prime find.
                    if (isPrime)
                    {
                        //Add the prime to the list.
                        Primes.Add(numberToCheck);

                        //Write the primes to a file for reading by the user.
                        if (Primes.Count % NumberOfPrimesInFile == 0)
                        {
                            var startIndex = NumberOfPrimesInFile * NextFileIndex - NumberOfPrimesInFile;
                            writePrimesToFile(Primes, startIndex, NumberOfPrimesInFile);
                        }
                    }

                    //Prepare checking the next number.
                    numberToCheck++;
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        /// <summary>
        /// Finds out if a number is a prime.
        /// </summary>
        /// <param name="allKnownPrimes">All known primes up to this point.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>TRUE if the number is a prime, else FALSE.</returns>
        private static bool isPrimeNumber(List<BigInteger> allKnownPrimes, BigInteger numberToCheck)
        {
            //Handle special cases.
            if (numberToCheck < 2) { return false; }
            if (numberToCheck == 2) { return true; }
            if (numberToCheck.IsEven) { return false; }

            //Store if the number is a prime.
            var isPrime = true;

            //Find out if the number is a prime.
            Parallel.ForEach(allKnownPrimes, (prime, state) =>
            {
                if (prime * prime >= numberToCheck)
                {
                    state.Break();
                }

                if (numberToCheck % prime == 0)
                {
                    isPrime = false;
                    state.Break();
                }
            });

            return isPrime;
        }

        /// <summary>
        /// Writes a number of primes to a file.
        /// </summary>
        /// <param name="allKnownPrimes">All known primes up to this point.</param>
        /// <param name="startIndex">The index of the first prime to write.</param>
        /// <param name="amountOfPrimesToWrite">The amount of primes to write.</param>
        private void writePrimesToFile(List<BigInteger> allKnownPrimes, int startIndex, int amountOfPrimesToWrite)
        {
            //Write the primes to a file.
            var primesToWrite = allKnownPrimes.GetRange(startIndex, amountOfPrimesToWrite);
            var filename = String.Format("PrimeNumbers{0}.txt", NextFileIndex);
            using (var stream = new StreamWriter(filename))
            {
                foreach (var prime in primesToWrite)
                {
                    stream.WriteLine(prime);
                }
            }
            var duration = DateTime.Now - LastFileWrite;
            LastFileWrite = DateTime.Now;

            //Write information message for user.
            var args = new PrimesWrittenToFileArgs(NextFileIndex, startIndex, startIndex + amountOfPrimesToWrite - 1, DateTime.Now, duration);
            OnPrimesWrittenToFile?.Invoke(this, args);

            NextFileIndex++;
        }
    }
}
