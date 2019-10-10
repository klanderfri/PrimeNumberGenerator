using System;
using System.Collections.Generic;
using System.Numerics;

namespace PrimeNumberGenerator
{
    public class ExistingPrimesLoadingResult
    {
        /// <summary>
        /// Tells if the loading had to be stopped.
        /// </summary>
        public bool ExecutionWasAborted { get; set; }

        /// <summary>
        /// The primes the loader managed to store in the memory.
        /// </summary>
        public List<BigInteger> CachedPrimes { get; set; }

        /// <summary>
        /// The next number to check if it is a prime number.
        /// </summary>
        public BigInteger NextNumberToCheck { get; set; }

        /// <summary>
        /// Tells if the memory has been filled and can't store any more prime numbers.
        /// </summary>
        public bool MemoryLimitReached { get; set; }

        /// <summary>
        /// The index of the last result file to store prime numbers in.
        /// </summary>
        /// <remarks>The file with this index may or may not exist.</remarks>
        public int IndexOfLastResultFileToStoreIn { get; set; }

        public ExistingPrimesLoadingResult()
        {
            CachedPrimes = new List<BigInteger>((int)Math.Pow(2, 20));
            NextNumberToCheck = new BigInteger(0);
            ExecutionWasAborted = false;
            MemoryLimitReached = false;
        }
    }
}
