namespace PrimeNumberGenerator.EventArgs
{
    public class PrimesLoadingFromFile : System.EventArgs
    {
        public int IndexOfFileToBeLoaded { get; private set; }
        public int TotalAmountOfFiles { get; private set; }

        public PrimesLoadingFromFile(int indexOfFileToBeLoaded, int totalAmountOfFiles)
        {
            IndexOfFileToBeLoaded = indexOfFileToBeLoaded;
            TotalAmountOfFiles = totalAmountOfFiles;
        }
    }
}
