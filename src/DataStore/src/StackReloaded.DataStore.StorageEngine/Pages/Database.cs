using System;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal sealed class Database : IDisposable
    {
        private DataFile primaryDataFile;

        private Database(DataFile primaryDataFile)
        {
            this.primaryDataFile = primaryDataFile;
        }

        public static Database Create(string filePath)
        {
            DataFile dataFile = null;
            try
            {
                dataFile = DataFile.CreateNewPrimary(filePath);
            }
            catch
            {
                dataFile?.Dispose();
                throw;
            }
            
            return new Database(dataFile);
        }

        public void Dispose()
        {
            if (this.primaryDataFile != null)
            {
                this.primaryDataFile.Dispose();
                this.primaryDataFile = null;
            }
        }
    }
}
