using System.IO;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal static class FileStreamExtensions
    {
        public static void FlushToDisk(this FileStream fs)
        {
            fs.Flush(flushToDisk: true);
        }
    }
}
