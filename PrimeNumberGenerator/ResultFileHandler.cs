using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimeNumberGenerator
{
    public class ResultFileHandler
    {
        /// <summary>
        /// Fetches the file paths of existing result files.
        /// </summary>
        /// <returns>The paths to the existing result files.</returns>
        public static List<KeyValuePair<int, string>> FetchResultFilePaths()
        {
            var files = new SortedDictionary<int, string>();

            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());
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
                .Where(f => f.Key == i++)
                .Select(f => f)
                .ToList();

            return consecutiveFiles;
        }
    }
}
