namespace PrimeNumberGenerator
{
    public class Configuration
    {
        /// <summary>
        /// The string the filenames of all prime number result files will start with.
        /// </summary>
        public static string ResultFileNameStart => "PrimeNumbers";

        /// <summary>
        /// The number of primes a single result file should hold as a maximum.
        /// </summary>
        public static int NumberOfPrimesInFile => 10000;

        /// <summary>
        /// The file extension of the prime number result files.
        /// </summary>
        public static string ResultFileExtension => ".txt";
    }
}
