using System;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal static class Size
    {
        public const int Kilobyte = 1024;
        public const int Megabyte = 1024 * Kilobyte;
        public const int Gigabyte = 1024 * Megabyte;
        public const long Terabyte = 1024 * (long)Gigabyte;

        public const int Sector = 512;

        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        public static string Format(long valueInBytes)
        {
            if (Math.Abs(valueInBytes) > OneTb)
            {
                return $"{Math.Round(valueInBytes / (double)OneTb, 4):#,#.####} TBytes";
            }
                
            if (Math.Abs(valueInBytes) > OneGb)
            {
                return $"{Math.Round(valueInBytes / (double)OneGb, 3):#,#.###} GBytes";
            }
                
            if (Math.Abs(valueInBytes) > OneMb)
            {
                return $"{Math.Round(valueInBytes / (double)OneMb, 2):#,#.##} MBytes";
            }
                
            if (Math.Abs(valueInBytes) > OneKb)
            {
                return $"{Math.Round(valueInBytes / (double)OneKb, 2):#,#.##} KBytes";
            }

            return $"{valueInBytes:#,#0} Bytes";
        }
    }
}
