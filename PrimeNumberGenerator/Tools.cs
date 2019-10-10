using System;

namespace PrimeNumberGenerator
{
    public class Tools
    {
        /// <summary>
        /// Checks if an exception indicates that the memory is filled with prime numbers.
        /// </summary>
        /// <param name="ex">The exception raised.</param>
        /// <returns>TRUE if the exception was raised due to memory filled with prime numbers, else FALSE.</returns>
        public static bool MemoryIsFilledWithPrimes(Exception ex)
        {
            return (ex is OutOfMemoryException) ? ex.TargetSite.Name == "set_Capacity" : false;
        }
    }
}
