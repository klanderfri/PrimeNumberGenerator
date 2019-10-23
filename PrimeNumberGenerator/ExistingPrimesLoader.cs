using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PrimeNumberGenerator.EventArgs;

namespace PrimeNumberGenerator
{
    public class ExistingPrimesLoader
    {
        #region Events

        /// <summary>
        /// Handler for the OnLoadingPrimesFromResultFileStarted-event.
        /// </summary>
        /// <param name="loader">The loader that has started loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileStartedHandler(object loader, LoadingPrimesFromResultFileStartedArgs args);

        /// <summary>
        /// Event raised when the generator has started loading primes from exisiting result files.
        /// </summary>
        public event LoadingPrimesFromResultFileStartedHandler OnLoadingPrimesFromResultFileStarted;

        /// <summary>
        /// Handler for the OnLoadingPrimesFromFile-event.
        /// </summary>
        /// <param name="loader">The loader responsible for loading the primes.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromFileHandler(object loader, PrimesLoadingFromFile args);

        /// <summary>
        /// Event raised when primes are to be loaded from a result file.
        /// </summary>
        public event LoadingPrimesFromFileHandler OnLoadingPrimesFromFile;

        /// <summary>
        /// Handler for the OnLoadingPrimesFromResultFileFinished-event.
        /// </summary>
        /// <param name="loader">The loader that has finished loading prime numbers.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void LoadingPrimesFromResultFileFinishedHandler(object loader, LoadingPrimesFromResultFileFinishedArgs args);

        /// <summary>
        /// Event raised when the generator has finished loading primes from exisiting result files.
        /// </summary>
        public event LoadingPrimesFromResultFileFinishedHandler OnLoadingPrimesFromResultFileFinished;

        #endregion

        /// <summary>
        /// Fetches prime numbers from existing result files.
        /// </summary>
        /// <param name="fileHandler">The object handling the result files.</param>
        /// <returns>Container holding information about the loaded prime numbers.</returns>
        public ExistingPrimesLoadingResult FetchExistingPrimes(ResultFileHandler fileHandler)
        {
            //Tell the user the loading has started.
            var startingArgs = new LoadingPrimesFromResultFileStartedArgs();
            OnLoadingPrimesFromResultFileStarted?.Invoke(this, startingArgs);

            //Load the existing primes.
            var result = fetchPrimes(fileHandler.ResultFiles);

            //Store the index of the last result file with room for more prime numbers.
            result.IndexOfLastResultFileToStoreIn = findFirstStorableFileIndex(fileHandler.ResultFiles);

            //Tell the user that the loading finished.
            int numberOfPrimesLoaded = result.CachedPrimes.Count;
            int numberOfResultFilesLoaded = result.CachedPrimes.Count / Configuration.NumberOfPrimesInFile;
            var loadingFinishedArgs = new LoadingPrimesFromResultFileFinishedArgs(numberOfPrimesLoaded, numberOfResultFilesLoaded);
            OnLoadingPrimesFromResultFileFinished?.Invoke(this, loadingFinishedArgs);

            //Return the result from the loading.
            return result;
        }

        /// <summary>
        /// Fetches prime numbers from existing result files.
        /// </summary>
        /// <param name="indexedResultFiles">All the result files accompanied with their index.</param>
        /// <returns>Container holding information about the loaded prime numbers.</returns>
        private ExistingPrimesLoadingResult fetchPrimes(SortedDictionary<int, string> indexedResultFiles)
        {
            var result = new ExistingPrimesLoadingResult();
            
            var resultFiles = indexedResultFiles
                .Select(f => f.Value)
                .ToList();
            var lastResultFile = resultFiles.LastOrDefault(f=>File.ReadLines(f).Any());

            for (int i = 0; i < resultFiles.Count; i++)
            {
                var loadingArgs = new PrimesLoadingFromFile(i, resultFiles.Count);
                OnLoadingPrimesFromFile?.Invoke(this, loadingArgs);

                var subPrimes = File
                    .ReadLines(resultFiles[i])
                    .Select(l => BigInteger.Parse(l));

                //Verify that the file contained any prime numbers.
                if (subPrimes.Any())
                {
                    //Calculate the next number to check if it's a prime number.
                    result.NextNumberToCheck = subPrimes.Last() + 1;
                }
                else
                {
                    //TODO: Test this use case.
                    return result;
                }

                //Store the loaded primes.
                result = storeSubPrimesInMemory(result, subPrimes, lastResultFile);

                //There's no use in loading any more primes if the memory is full.
                if (result.MemoryLimitReached)
                {
                    return result;
                }

                //Check if the user cancelled the execution.
                if (userPressedEscape())
                {
                    result.ExecutionWasAborted = true;
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds The index of the first result file one could store prime numbers in.
        /// </summary>
        /// <param name="resultFiles">A list of all existing result files.</param>
        /// <returns>The index of the last result file with room left for more prime numbers.</returns>
        private static int findFirstStorableFileIndex(IEnumerable<KeyValuePair<int, string>> resultFiles)
        {
            if (!resultFiles.Any())
            {
                return 1;
            }

            var lastFile = resultFiles
                .OrderByDescending(f => f.Key)
                .First(f => File.ReadLines(f.Value).Any());

            var fileHasRoomLeft = File.ReadLines(lastFile.Value).Count() < Configuration.NumberOfPrimesInFile;
            return fileHasRoomLeft ? lastFile.Key : lastFile.Key + 1;
        }

        /// <summary>
        /// Checks if the user pressed the Escape key.
        /// </summary>
        /// <returns>TRUE if the user pressed the Escape key, else FALSE.</returns>
        private static bool userPressedEscape()
        {
            return Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape;
        }

        /// <summary>
        /// Adds the loaded sub primes to the container holding all the loaded prime numbers.
        /// </summary>
        /// <param name="result">The loading result container.</param>
        /// <param name="subPrimes">The prime numbers to add to the container.</param>
        /// <param name="lastResultFile">The last result file.</param>
        /// <returns>The loading result container.</returns>
        private static ExistingPrimesLoadingResult storeSubPrimesInMemory(ExistingPrimesLoadingResult result, IEnumerable<BigInteger> subPrimes, string lastResultFile)
        {
            try
            {
                result.CachedPrimes.AddRange(subPrimes);
            }
            catch (OutOfMemoryException ex)
            {
                if (Tools.MemoryIsFilledWithPrimes(ex))
                {
                    return handleMemoryOverflow(result, lastResultFile);
                }

                throw;
            }

            return result;
        }

        /// <summary>
        /// Handles the event of the computer memory being filled with loaded prime numbers.
        /// </summary>
        /// <param name="result">The loading result container.</param>
        /// <param name="lastResultFile">The last result file.</param>
        /// <returns>The loading result container.</returns>
        private static ExistingPrimesLoadingResult handleMemoryOverflow(ExistingPrimesLoadingResult result, string lastResultFile)
        {
            result.MemoryLimitReached = true;

            //There was a number when the result files were generated.
            //That number was the next in line to be checked if it was a prime number.
            //Find that number again.
            var lastPrime = File
                .ReadLines(lastResultFile)
                .Select(l => BigInteger.Parse(l))
                .LastOrDefault();
            var nextNumberToCheck = lastPrime + 1;

            //The next number to check we calculated from the last prime in the last file should be bigger or equal
            //to the corresponding number in the result container, otherwise somethings are wrong with the result files.
            if (nextNumberToCheck < result.NextNumberToCheck)
            {
                var message = "Calculation indicates corrupted result files. Make sure all result files exists and that the prime numbers within are sorted ascending.";
                throw new InvalidOperationException(message);
            }

            result.NextNumberToCheck = nextNumberToCheck;

            return result;
        }
    }
}
