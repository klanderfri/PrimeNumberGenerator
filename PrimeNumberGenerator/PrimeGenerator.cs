using PrimeNumberGenerator.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Handler for the OnLoadingPrimesFromResultFileStarted-event.
        /// </summary>
        /// <param name="generator">The generator that has started loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileStartedHandler(object generator, LoadingPrimesFromResultFileStartedArgs args);

        /// <summary>
        /// Handler for the OnLoadingPrimesFromResultFileFinished-event.
        /// </summary>
        /// <param name="generator">The generator that has finished loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileFinishedHandler(object generator, LoadingPrimesFromResultFileFinishedArgs args);

        /// <summary>
        /// Handler for the OnPrimeGenerationStarted-event.
        /// </summary>
        /// <param name="generator">The generator that has started generating prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void PrimeGenerationStartedHandler(object generator, PrimeGenerationStartedArgs args);

        /// <summary>
        /// Handler for the OnPrimesWrittenToFile-event.
        /// </summary>
        /// <param name="generator">The generator causing the result-file to be written.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void PrimesWrittenToFileHandler(object generator, PrimesWrittenToFileArgs args);

        /// <summary>
        /// Event raised when the generator has started loading primes from exisiting result files.
        /// </summary>
        public event LoadingPrimesFromResultFileStartedHandler OnLoadingPrimesFromResultFileStarted;

        /// <summary>
        /// Event raised when the generator has finished loading primes from exisiting result files.
        /// </summary>
        public event LoadingPrimesFromResultFileFinishedHandler OnLoadingPrimesFromResultFileFinished;

        /// <summary>
        /// Event raised when the prime number generation has started.
        /// </summary>
        public event PrimeGenerationStartedHandler OnPrimeGenerationStarted;

        /// <summary>
        /// Event raised when primes are written to a result-file.
        /// </summary>
        public event PrimesWrittenToFileHandler OnPrimesWrittenToFile;

        /// <summary>
        /// The string the filenames of all prime number result files will start with.
        /// </summary>
        public const string RESULT_FILE_NAME_START = "PrimeNumbers";

        /// <summary>
        /// Creates an object generating prime numbers.
        /// </summary>
        public PrimeGenerator(int numberOfPrimesInFile)
        {
            NumberOfPrimesInFile = numberOfPrimesInFile;
        }

        /// <summary>
        /// Starts the prime number generation.
        /// </summary>
        public void GeneratePrimes()
        {
            //Load existing primes.
            OnLoadingPrimesFromResultFileStarted?.Invoke(this, new LoadingPrimesFromResultFileStartedArgs());
            Primes = fetchExistingPrimes();
            var loadingFinishedArgs = new LoadingPrimesFromResultFileFinishedArgs(Primes.Count, Primes.Count / NumberOfPrimesInFile);
            OnLoadingPrimesFromResultFileFinished?.Invoke(this, loadingFinishedArgs);

            //Setup variables for prime number generation.
            NextFileIndex = Primes.Count / NumberOfPrimesInFile + 1;
            var numberToCheck = Primes.LastOrDefault() + 1;
            LastFileWrite = DateTime.Now;

            try
            {
                //Inform the subscriber that the prime number generation has started.
                OnPrimeGenerationStarted?.Invoke(this, new PrimeGenerationStartedArgs());

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
            catch (Exception ex)
            {
                ex.Data.Add("CurrentNumberToCheck", numberToCheck);

                //Rethrow the exception.
                throw;
            }
        }

        /// <summary>
        /// Finds out if a number is a prime.
        /// </summary>
        /// <param name="allKnownPrimesSortedAsc">All known primes up to this point, sorted from small to big.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>TRUE if the number is a prime, else FALSE.</returns>
        private static bool isPrimeNumber(List<BigInteger> allKnownPrimesSortedAsc, BigInteger numberToCheck)
        {
            //Handle special cases.
            if (numberToCheck < 2) { return false; }
            if (numberToCheck == 2) { return true; }
            if (numberToCheck.IsEven) { return false; }

            //Store if the number is a prime.
            var isPrime = true;

            //Find the primes small enough to be a factor.
            List<BigInteger> primesToUse = findPrimesToUse(allKnownPrimesSortedAsc, numberToCheck);

            //Find out if the number is a prime.
            Parallel.ForEach(primesToUse, (prime, state) =>
            {
                if (numberToCheck % prime == 0)
                {
                    isPrime = false;
                    state.Break();
                }
            });

            return isPrime;
        }

        /// <summary>
        /// Finds the primes needed to check if a certain number is a prime.
        /// </summary>
        /// <param name="allKnownPrimesSortedAsc">All known primes up to this point, sorted from small to big.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>A List of primes needed to check if a certain number is a prime.</returns>
        private static List<BigInteger> findPrimesToUse(List<BigInteger> allKnownPrimesSortedAsc, BigInteger numberToCheck)
        {
            if (allKnownPrimesSortedAsc.Count < 2)
            {
                return allKnownPrimesSortedAsc;
            }

            var upperLimit = allKnownPrimesSortedAsc.Count() - 1;
            var lowerLimit = 0;

            do
            {
                var middleIndex = (upperLimit - lowerLimit) / 2 + lowerLimit;
                var middlePrime = allKnownPrimesSortedAsc.ElementAt(middleIndex);
                var square = middlePrime * middlePrime;

                if (square < numberToCheck)
                {
                    lowerLimit = middleIndex;
                }
                else if (square > numberToCheck)
                {
                    upperLimit = middleIndex;
                }
                else //They are equal, we accidentally found a factor.
                {
                    return new List<BigInteger>() { middlePrime };
                }

            } while (upperLimit - lowerLimit > 1);

            return allKnownPrimesSortedAsc.GetRange(0, upperLimit);
        }

        /// <summary>
        /// Writes a number of primes to a file.
        /// </summary>
        /// <param name="allKnownPrimes">All known primes up to this point.</param>
        /// <param name="startIndex">The index of the first prime to write.</param>
        /// <param name="amountOfPrimesToWrite">The amount of primes to write.</param>
        /// TODO: Move to the program file and replace with event. The generator shouldn't handle file input/output.
        private void writePrimesToFile(List<BigInteger> allKnownPrimes, int startIndex, int amountOfPrimesToWrite)
        {
            //Write the primes to a file.
            var primesToWrite = allKnownPrimes.GetRange(startIndex, amountOfPrimesToWrite);
            var filename = String.Format("{0}{1}.txt", RESULT_FILE_NAME_START, NextFileIndex);
            using (var stream = new StreamWriter(filename))
            {
                foreach (var prime in primesToWrite)
                {
                    stream.WriteLine(prime);
                }
            }
            var duration = DateTime.Now - LastFileWrite;
            LastFileWrite = DateTime.Now;

            //Inform the subscriber that a result file was created.
            var args = new PrimesWrittenToFileArgs(NextFileIndex, startIndex, startIndex + amountOfPrimesToWrite - 1, DateTime.Now, duration);
            OnPrimesWrittenToFile?.Invoke(this, args);

            NextFileIndex++;
        }

        /// <summary>
        /// Fetches prime numbers from existing result files.
        /// </summary>
        /// <returns>The already generated primes.</returns>
        private static List<BigInteger> fetchExistingPrimes()
        {
            var primes = new List<BigInteger>((int)Math.Pow(2, 20));

            foreach (var file in fetchResultFilePaths())
            {
                var subPrimes = File
                    .ReadAllLines(file)
                    .Select(l => BigInteger.Parse(l));

                primes.AddRange(subPrimes);
            }

            return primes;
        }

        /// <summary>
        /// Fetches the file paths of existing result files.
        /// </summary>
        /// <returns>The paths to the existing result files.</returns>
        private static List<string> fetchResultFilePaths()
        {
            var files = new SortedDictionary<int, string>();

            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());
            foreach (var filepath in allFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filepath);

                if (fileName.Length <= RESULT_FILE_NAME_START.Length
                    || !fileName.StartsWith(RESULT_FILE_NAME_START)
                    || Path.GetExtension(filepath) != ".txt")
                {
                    continue;
                }

                var fileID = fileName.Substring(RESULT_FILE_NAME_START.Length);
                int fileNumber;

                if (!int.TryParse(fileID, out fileNumber))
                {
                    continue;
                }

                files.Add(fileNumber, filepath);
            }

            var i = 1;
            var consecutiveFiles = files
                .Where(f => f.Key == i++)
                .Select(f => f.Value)
                .ToList();

            return consecutiveFiles;
        }
    }
}
