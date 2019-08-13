namespace PrimeNumberGenerator.EventArgs
{
    public class LoadingPrimesFromResultFileFinishedArgs : System.EventArgs
    {
        public int NumberOfPrimesLoaded { get; private set; }
        public int NumberOfResultFilesLoaded { get; private set; }

        public LoadingPrimesFromResultFileFinishedArgs(int numberOfPrimesLoaded, int numberOfResultFilesLoaded)
        {
            NumberOfPrimesLoaded = numberOfPrimesLoaded;
            NumberOfResultFilesLoaded = numberOfResultFilesLoaded;
        }
    }
}