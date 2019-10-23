using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PrimeNumberGenerator.EventArgs;

namespace PrimeNumberGenerator
{
    public class ResultFileHandler
    {
        /// <summary>
        /// Handler for the OnPrimesWrittenToFile-event.
        /// </summary>
        /// <param name="handler">The handler causing the result-file to be written.</param>
        /// <param name="args">The information about the event.</param>
        public delegate void PrimesWrittenToFileHandler(object handler, PrimesWrittenToFileArgs args);
        
        /// <summary>
        /// Event raised when primes are written to a result-file.
        /// </summary>
        public event PrimesWrittenToFileHandler OnPrimesWrittenToFile;

        /// <summary>
        /// The point in time when the last result-file was written.
        /// </summary>
        private DateTime LastFileWrite;

        /// <summary>
        /// The list of the available prime number result files.
        /// </summary>
        public SortedDictionary<int, string> ResultFiles { get; private set; }

        public ResultFileHandler()
        {
            ResultFiles = fetchResultFilePaths();
        }

        /// <summary>
        /// Method to call to tell the handler that the prime number generation has started.
        /// </summary>
        public void PrimeNumberGenerationHasStarted()
        {
            LastFileWrite = DateTime.Now;
        }

        /// <summary>
        /// Fetches the file paths of existing result files.
        /// </summary>
        /// <returns>The paths to the existing result files.</returns>
        private SortedDictionary<int, string> fetchResultFilePaths()
        {
            var files = new Dictionary<int, string>();

            var allFiles = Directory.GetFiles(getResultFileFolder());
            foreach (var filepath in allFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filepath);

                if (fileName.Length <= Configuration.ResultFileNameStart.Length
                    || !fileName.StartsWith(Configuration.ResultFileNameStart)
                    || Path.GetExtension(filepath) != Configuration.ResultFileExtension)
                {
                    continue;
                }

                var fileID = fileName.Substring(Configuration.ResultFileNameStart.Length);
                int fileNumber;

                if (!int.TryParse(fileID, out fileNumber))
                {
                    continue;
                }

                files.Add(fileNumber, filepath);
            }

            var i = 1;
            var consecutiveFiles = files
                .OrderBy(f => f.Key)
                .Where(f => f.Key == i++)
                .Select(f => f)
                .ToDictionary(e => e.Key, e => e.Value);

            return new SortedDictionary<int, string>(consecutiveFiles);
        }

        /// <summary>
        /// Creates an exception for informing that a file has to many prime numbers in it.
        /// </summary>
        /// <param name="lastFile">The information about the file with to many prime numbers in it.</param>
        /// <returns>The exception created.</returns>
        private static InvalidOperationException createToManyLinesException(KeyValuePair<int, string> lastFile)
        {
            var format = "The result file with index '{0}' contains more primes than the allowed {1}.";
            var message = String.Format(format, lastFile.Key, Configuration.NumberOfPrimesInFile);
            var oops = new InvalidOperationException(message);
            oops.Data.Add("OverfilledFileIndex", lastFile.Key);
            oops.Data.Add("OverfilledFilePath", lastFile.Value);

            return oops;
        }

        /// <summary>
        /// Adds an empty prime number result file that could be used for storing primes.
        /// </summary>
        /// <param name="fileIndex">The index of the new empty result file.</param>
        private void addEmptyResultFile(int fileIndex)
        {
            var filename = String.Format("{0}{1}{2}", Configuration.ResultFileNameStart, fileIndex, Configuration.ResultFileExtension);
            var filepath = Path.Combine(getResultFileFolder(), filename);

            File.Delete(filepath);
            ResultFiles.Add(fileIndex, filepath);
        }

        /// <summary>
        /// Gets the path to the folder holding the prime number result files.
        /// </summary>
        /// <returns>The full path of the result file folder.</returns>
        private string getResultFileFolder()
        {
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Makes sure there is a file ready for storing prime numbers in.
        /// </summary>
        private void prepareFileForWriting()
        {
            if (ResultFiles.Any())
            {
                var lastFile = ResultFiles.Last();
                var lines = File.ReadLines(lastFile.Value);
                var amountOfLines = lines.Count();

                //Check if the last result file is full.
                if (amountOfLines == Configuration.NumberOfPrimesInFile)
                {
                    //Add a new file for storing primes in.
                    addEmptyResultFile(lastFile.Key + 1);
                }
                else if (amountOfLines > Configuration.NumberOfPrimesInFile)
                {
                    throw createToManyLinesException(lastFile);
                }
            }
            else
            {
                addEmptyResultFile(1);
            }
        }

        /// <summary>
        /// Writes a number of primes to a file.
        /// </summary>
        /// <param name="primesToWrite">The primes to write to a file.</param>
        public void WritePrimesToFile(List<BigInteger> primesToWrite)
        {
            KeyValuePair<int, string> lastFile;
            int amountOfLines, startIndex;
            StreamWriter stream;

            prepareFileForWriting();
            openStream();

            try
            {
                for (int i = 0; i < primesToWrite.Count; i++)
                {
                    stream.WriteLine(primesToWrite[i]);
                    amountOfLines++;

                    var fileIsFilled = (amountOfLines == Configuration.NumberOfPrimesInFile);
                    var fileIsOverfilled = (amountOfLines > Configuration.NumberOfPrimesInFile);
                    var hasMorePrimesToWrite = (i < primesToWrite.Count - 1);

                    if (fileIsFilled && hasMorePrimesToWrite)
                    {
                        addEmptyResultFile(lastFile.Key + 1);

                        closeStream();
                        openStream();
                    }
                    else if (fileIsOverfilled)
                    {
                        createToManyLinesException(lastFile);
                    }
                }
            }
            finally
            {
                closeStream();
            }

            void openStream()
            {
                //Fetch the last file.
                lastFile = ResultFiles.Last();

                //Find out how many rows there are already written.
                amountOfLines = File.Exists(lastFile.Value) ? File.ReadLines(lastFile.Value).Count() : 0;

                //Open the stream.
                stream = new StreamWriter(lastFile.Value, true);

                //Update the index of the first prime in the file.
                startIndex = (ResultFiles.Count - 1) * Configuration.NumberOfPrimesInFile + amountOfLines;
            }

            void closeStream()
            {
                //Close the stream.
                stream.Close();

                //Calculate how long the writing took.
                var duration = DateTime.Now - LastFileWrite;
                LastFileWrite = DateTime.Now;

                //Inform the subscriber that the primes was written to a file.
                var endIndex = startIndex + primesToWrite.Count;
                var args = new PrimesWrittenToFileArgs(lastFile.Key, startIndex, endIndex, DateTime.Now, duration);
                OnPrimesWrittenToFile?.Invoke(this, args);
            }
        }
    }
}
