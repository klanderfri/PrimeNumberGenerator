using System;

namespace PrimeNumberGenerator.EventArgs
{
    public class PrimesWrittenToFileArgs : System.EventArgs
    {
        public int FileIndex { get; private set; }
        public int StartPrimeIndex { get; private set; }
        public int EndPrimeIndex { get; private set; }
        public DateTime WriteTime { get; private set; }
        public TimeSpan GenerationDuration { get; private set; }

        public PrimesWrittenToFileArgs(int fileIndex, int startPrimeIndex, int endPrimeIndex, DateTime writeTime, TimeSpan generationDuration)
        {
            FileIndex = fileIndex;
            StartPrimeIndex = startPrimeIndex;
            EndPrimeIndex = endPrimeIndex;
            WriteTime = writeTime;
            GenerationDuration = generationDuration;
        }
    }
}
