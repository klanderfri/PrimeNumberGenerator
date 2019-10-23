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
        /// The first prime numbers, cached in computer memory.
        /// </summary>
        public List<BigInteger> ChachedPrimes { get; private set; }

        /// <summary>
        /// Tells if the memory limit is reached, indiciating the memory can't hold any more prime numbers.
        /// </summary>
        private bool MemoryLimitReached { get; set; }

        /// <summary>
        /// Helper handling the files containing prime numbers.
        /// </summary>
        private ResultFileHandler FileHandler { get; set; }

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
        /// <param name="handler">The handler causing the result-file to be written.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void PrimesWrittenToFileHandler(object handler, PrimesWrittenToFileArgs args);

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
            //Set the handler handling the result files.
            FileHandler = new ResultFileHandler();
            FileHandler.OnPrimesWrittenToFile += FileHandler_OnPrimesWrittenToFile;

            //Load existing primes.
            var loader = new ExistingPrimesLoader();
            loader.OnLoadingPrimesFromResultFileStarted += Loader_OnLoadingPrimesFromResultFileStarted;
            loader.OnLoadingPrimesFromResultFileFinished += Loader_OnLoadingPrimesFromResultFileFinished;
            loader.OnLoadingPrimesFromFile += Loader_OnLoadingPrimesFromFile;
            var loadResult = loader.FetchExistingPrimes(FileHandler);
            
            //End the execution if the user aborted it.
            if (loadResult.ExecutionWasAborted) { return; }

            //Extract the data from the loading result.
            ChachedPrimes = loadResult.CachedPrimes;
            MemoryLimitReached = loadResult.MemoryLimitReached;
            var numberToCheck = loadResult.NextNumberToCheck;

            //Setup variables for prime number generation.
            bool isPrime = false;

            try
            {
                //Inform the subscriber that the prime number generation has started.
                OnPrimeGenerationStarted?.Invoke(this, new PrimeGenerationStartedArgs());
                FileHandler.PrimeNumberGenerationHasStarted();

                //Skip memory stored generation if the memory was filled by the already generated prime numbers.
                if (!MemoryLimitReached)
                {
                    //Create variable keeping track of were the unstored primes begin.
                    var indexOfFirstUnstoredPrime = ChachedPrimes.Count;

                    //Generate the first primes quickly by using only the computer memory.
                    Func<bool> memoryIteration = delegate ()
                    {
                        //Check if the number is a prime.
                        isPrime = PrimeChecker.IsPrimeNumber(ChachedPrimes, numberToCheck);

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
                                writePrimesToFile(ChachedPrimes, indexOfFirstUnstoredPrime, Configuration.NumberOfPrimesInFile);
                                indexOfFirstUnstoredPrime = ChachedPrimes.Count;
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
                    storeUnsavedPrimesAfterMemoryOverflow(ChachedPrimes, numberToCheck);
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
        /// Stores the unsaved primes to file after a memory overflow occured.
        /// </summary>
        /// <param name="foundPrimes">The container holding the prime numbers that have been found.</param>
        /// <param name="primeCausingMemoryOverflow">The prime number that coused the memory overflow.</param>
        private void storeUnsavedPrimesAfterMemoryOverflow(List<BigInteger> foundPrimes, BigInteger primeCausingMemoryOverflow)
        {
            //Find out how many primes are waiting to be stored in file.
            var amountOfUnstoredPrimes = foundPrimes.Count % Configuration.NumberOfPrimesInFile;
            
            //Store the unsaved primes.
            if (amountOfUnstoredPrimes > 0)
            {
                var indexOfFirstUnstoredPrime = foundPrimes.Count - amountOfUnstoredPrimes;
                writePrimesToFile(foundPrimes, indexOfFirstUnstoredPrime, amountOfUnstoredPrimes);
            }

            //Store the prime that cause the memory overflow.
            writePrimesToFile(new List<BigInteger>() { primeCausingMemoryOverflow }, 0, 1);
        }

        /// <summary>
        /// Handles the OnPrimesWrittenToFile event for the object handling result files.
        /// </summary>
        /// <param name="handler">The object writing prime numbers to file.</param>
        /// <param name="args">The information about the event.</param>
        private void FileHandler_OnPrimesWrittenToFile(object handler, PrimesWrittenToFileArgs args)
        {
            OnPrimesWrittenToFile?.Invoke(handler, args);
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromResultFileStarted event for the object loading existing prime numbers.
        /// </summary>
        /// <param name="loader">The object loading the existing prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        private void Loader_OnLoadingPrimesFromResultFileStarted(object loader, LoadingPrimesFromResultFileStartedArgs args)
        {
            OnLoadingPrimesFromResultFileStarted?.Invoke(loader, args);
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromResultFileFinished event for the object loading existing prime numbers.
        /// </summary>
        /// <param name="loader">The object loading the existing prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        private void Loader_OnLoadingPrimesFromResultFileFinished(object loader, LoadingPrimesFromResultFileFinishedArgs args)
        {
            OnLoadingPrimesFromResultFileFinished?.Invoke(loader, args);
        }

        /// <summary>
        /// Handles the OnLoadingPrimesFromFile event for the object loading existing prime numbers.
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
        /// Writes a number of primes to a file.
        /// </summary>
        /// <param name="primeNumberCollection">A range of prime numbers of which some should be written to a file.</param>
        /// <param name="startIndex">The index of the first prime to write.</param>
        /// <param name="amountOfPrimesToWrite">The amount of primes to write.</param>
        private void writePrimesToFile(List<BigInteger> primeNumberCollection, int startIndex, int amountOfPrimesToWrite)
        {
            var primesToWrite = primeNumberCollection.GetRange(startIndex, amountOfPrimesToWrite);
            FileHandler.WritePrimesToFile(primesToWrite);
        }
    }
}
