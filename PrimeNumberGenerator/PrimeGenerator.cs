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
        /// The index of the next result-file.
        /// </summary>
        private int NextFileIndex;

        /// <summary>
        /// The point in time when the last result-file was written.
        /// </summary>
        private DateTime LastFileWrite;

        /// <summary>
        /// The first prime numbers, cached in computer memory.
        /// </summary>
        public List<BigInteger> ChachedPrimes { get; private set; }

        /// <summary>
        /// Tells if the memory limit is reached, indiciating the memory can't hold any more prime numbers.
        /// </summary>
        private bool MemoryLimitReached { get; set; }

        #region Events

        /// <summary>
        /// Handler for the OnLoadingPrimesFromResultFileStarted-event.
        /// </summary>
        /// <param name="loader">The loader that has started loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileStartedHandler(object loader, LoadingPrimesFromResultFileStartedArgs args);

        /// <summary>
        /// Handler for the OnLoadingPrimesFromResultFileFinished-event.
        /// </summary>
        /// <param name="loader">The loader that has finished loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileFinishedHandler(object loader, LoadingPrimesFromResultFileFinishedArgs args);

        /// <summary>
        /// Handler for the OnLoadingPrimesFromFile-event.
        /// </summary>
        /// <param name="loader">The loader causing the primes being loaded.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromFileHandler(object loader, PrimesLoadingFromFile args);

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
        /// Event raised when primes are to be loaded from a result file.
        /// </summary>
        public event LoadingPrimesFromFileHandler OnLoadingPrimesFromFile;

        /// <summary>
        /// Event raised when the prime number generation has started.
        /// </summary>
        public event PrimeGenerationStartedHandler OnPrimeGenerationStarted;

        /// <summary>
        /// Event raised when primes are written to a result-file.
        /// </summary>
        public event PrimesWrittenToFileHandler OnPrimesWrittenToFile;

        #endregion

        /// <summary>
        /// Creates an object generating prime numbers.
        /// </summary>
        public PrimeGenerator()
        {
            MemoryLimitReached = false;
        }

        /// <summary>
        /// Starts the prime number generation.
        /// </summary>
        public void GeneratePrimes()
        {
            //Load existing primes.
            var loader = new ExistingPrimesLoader();
            loader.OnLoadingPrimesFromResultFileStarted += Loader_OnLoadingPrimesFromResultFileStarted;
            loader.OnLoadingPrimesFromResultFileFinished += Loader_OnLoadingPrimesFromResultFileFinished;
            loader.OnLoadingPrimesFromFile += Loader_OnLoadingPrimesFromFile;
            var loadResult = loader.FetchExistingPrimes();
            
            //End the execution if the user aborted it.
            if (loadResult.ExecutionWasAborted) { return; }

            //Extract the data from the loading result.
            ChachedPrimes = loadResult.CachedPrimes;
            MemoryLimitReached = loadResult.MemoryLimitReached;
            NextFileIndex = loadResult.IndexOfLastResultFileToStoreIn;
            var numberToCheck = loadResult.NextNumberToCheck;

            //Setup variables for prime number generation.
            LastFileWrite = DateTime.Now;
            bool isPrime = false;

            try
            {
                //Inform the subscriber that the prime number generation has started.
                OnPrimeGenerationStarted?.Invoke(this, new PrimeGenerationStartedArgs());

                //Skip memory stored generation if the memory was filled by the already generated prime numbers.
                if (!MemoryLimitReached)
                {
                    //Generate the first primes quickly by using only the computer memory.
                    Func<bool> memoryIteration = delegate ()
                    {
                        //Check if the number is a prime.
                        isPrime = isPrimeNumber(ChachedPrimes, numberToCheck);

                        //Handle prime find.
                        if (isPrime)
                        {
                            try
                            {
                                //Add the prime to the list.
                                ChachedPrimes.Add(numberToCheck);
                            }
                            catch (OutOfMemoryException ex)
                            {
                                if (Tools.MemoryIsFilledWithPrimes(ex))
                                {
                                    //We have reached the limit for how many primes the computer memory can hold.
                                    MemoryLimitReached = true;

                                    //Stop using the computer memory when generating prime numbers.
                                    return false;
                                }
                                else
                                {
                                    //Unknown error. Keep throwing.
                                    throw;
                                }
                            }

                            //Write the primes to a file for reading by the user.
                            if (ChachedPrimes.Count % Configuration.NumberOfPrimesInFile == 0)
                            {
                                var startIndex = Configuration.NumberOfPrimesInFile * NextFileIndex - Configuration.NumberOfPrimesInFile;
                                writePrimesToFile(ChachedPrimes, startIndex, Configuration.NumberOfPrimesInFile, NextFileIndex, false);
                                NextFileIndex++;
                            }
                        }

                        //Prepare checking the next number.
                        numberToCheck++;

                        //Keep generating prime numbers.
                        return true;
                    };
                    runInfiniteLoopUntilEscapeIsPressed(memoryIteration);

                    //Unless the memory limit is reached, it was the user who stopped the generation.
                    if (!MemoryLimitReached) { return; }

                    //Store the unstored primes.
                    long amountOfPrimesFound = ChachedPrimes.Count;
                    var amountOfUnstoredPrimes = ChachedPrimes.Count % Configuration.NumberOfPrimesInFile;
                    if (amountOfUnstoredPrimes > 0)
                    {
                        var indexOfFirstUnstoredPrime = ChachedPrimes.Count - amountOfUnstoredPrimes;
                        writePrimesToFile(ChachedPrimes, indexOfFirstUnstoredPrime, amountOfUnstoredPrimes, NextFileIndex, false);
                    }

                    //Store the prime that cause the memory overflow.
                    writePrimesToFile(new List<BigInteger>() { numberToCheck }, 0, 1, NextFileIndex, true);
                    amountOfPrimesFound++;

                    //Check if we have filled the file and need to continue on another.
                    if (amountOfPrimesFound % Configuration.NumberOfPrimesInFile == 0)
                    {
                        NextFileIndex++;
                    }
                }

                //Generate the rest of the primes by using the harddrive as memory.
                Func<bool> diskIteration = delegate ()
                {
                    throw new NotImplementedException();
                };
                runInfiniteLoopUntilEscapeIsPressed(diskIteration);
            }
            catch (Exception ex)
            {
                ex.Data.Add("CurrentNumberToCheck", numberToCheck);

                //Rethrow the exception.
                throw;
            }
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromResultFileStarted for the object loading existing prime numbers.
        /// </summary>
        /// <param name="loader">The object loading the existing prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        private void Loader_OnLoadingPrimesFromResultFileStarted(object loader, LoadingPrimesFromResultFileStartedArgs args)
        {
            OnLoadingPrimesFromResultFileStarted?.Invoke(loader, args);
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromResultFileFinished for the object loading existing prime numbers.
        /// </summary>
        /// <param name="loader">The object loading the existing prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        private void Loader_OnLoadingPrimesFromResultFileFinished(object loader, LoadingPrimesFromResultFileFinishedArgs args)
        {
            OnLoadingPrimesFromResultFileFinished?.Invoke(loader, args);
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromFile for the object loading existing prime numbers.
        /// </summary>
        /// <param name="loader">The object loading the existing prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        private void Loader_OnLoadingPrimesFromFile(object loader, PrimesLoadingFromFile args)
        {
            OnLoadingPrimesFromFile?.Invoke(loader, args);
        }

        /// <summary>
        /// Runs an infinitive loop until the user presses the Escape-key.
        /// </summary>
        /// <param name="iteration">The operation representing a single iteration.</param>
        private void runInfiniteLoopUntilEscapeIsPressed(Func<bool> iteration)
        {
            do
            {
                while (!Console.KeyAvailable)
                {
                    var keepLooping = iteration();

                    if (!keepLooping) { return; }
                }

            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        /// <summary>
        /// Finds out if a number is a prime.
        /// </summary>
        /// <param name="cachedPrimesSortedAsc">The first primes, sorted from small to big.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>TRUE if the number is a prime, else FALSE.</returns>
        private static bool isPrimeNumber(List<BigInteger> cachedPrimesSortedAsc, BigInteger numberToCheck)
        {
            //Handle special cases.
            if (numberToCheck < 2) { return false; }
            if (numberToCheck == 2) { return true; }
            if (numberToCheck.IsEven) { return false; }

            //Store if the number is a prime.
            var isPrime = true;

            //Find the primes small enough to be a factor.
            List<BigInteger> primesToUse = findPrimesToUse(cachedPrimesSortedAsc, numberToCheck);

            //Find out if the number is a prime.
            Parallel.ForEach(primesToUse, (prime, state) =>
            {
                if (numberToCheck % prime == 0)
                {
                    isPrime = false;
                    state.Stop();
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
        /// <param name="newFileIndex">The index the new result file should have.</param>
        /// <param name="theFileShouldExists">Tells if the file is expected to already exist.</param>
        /// TODO: Move to the program file and replace with event. The generator shouldn't handle file input/output.
        private void writePrimesToFile(List<BigInteger> allKnownPrimes, int startIndex, int amountOfPrimesToWrite, int newFileIndex, bool theFileShouldExists)
        {
            //Write the primes to a file.
            var primesToWrite = allKnownPrimes.GetRange(startIndex, amountOfPrimesToWrite);
            var filename = String.Format("{0}{1}{2}", Configuration.ResultFileNameStart, newFileIndex, Configuration.ResultFileExtension);

            if (!theFileShouldExists && File.Exists(filename))
            {
                var format = "Tried to write to the existing file '{0}'.";
                var message = String.Format(format, filename);
                throw new InvalidOperationException(message);
            }

            using (var stream = new StreamWriter(filename, true))
            {
                foreach (var prime in primesToWrite)
                {
                    stream.WriteLine(prime);
                }
            }
            var duration = DateTime.Now - LastFileWrite;
            LastFileWrite = DateTime.Now;

            //Inform the subscriber that a result file was created.
            var args = new PrimesWrittenToFileArgs(newFileIndex, startIndex, startIndex + amountOfPrimesToWrite - 1, DateTime.Now, duration);
            OnPrimesWrittenToFile?.Invoke(this, args);
        }
    }
}
