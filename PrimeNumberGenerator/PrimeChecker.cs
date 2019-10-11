﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PrimeNumberGenerator
{
    public class PrimeChecker
    {
        /// <summary>
        /// Finds out if a number is a prime.
        /// </summary>
        /// <param name="cachedPrimesSortedAsc">The first primes, sorted from small to big.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>TRUE if the number is a prime, else FALSE.</returns>
        public static bool IsPrimeNumber(List<BigInteger> cachedPrimesSortedAsc, BigInteger numberToCheck)
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
        /// <param name="cachedPrimesSortedAsc">The first primes, sorted from small to big.</param>
        /// <param name="numberToCheck">The number that may be a prime.</param>
        /// <returns>A List of primes needed to check if a certain number is a prime.</returns>
        private static List<BigInteger> findPrimesToUse(List<BigInteger> cachedPrimesSortedAsc, BigInteger numberToCheck)
        {
            if (cachedPrimesSortedAsc.Count < 2)
            {
                return cachedPrimesSortedAsc;
            }

            var upperLimit = cachedPrimesSortedAsc.Count() - 1;
            var lowerLimit = 0;

            do
            {
                var middleIndex = (upperLimit - lowerLimit) / 2 + lowerLimit;
                var middlePrime = cachedPrimesSortedAsc.ElementAt(middleIndex);
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

            return cachedPrimesSortedAsc.GetRange(0, upperLimit);
        }
    }
}