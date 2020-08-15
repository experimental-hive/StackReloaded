using System;
using System.Globalization;
using System.IO;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal class DataFile : IDisposable
    {
        private readonly string filePath;
        private readonly bool inMemory;

        private DataStream dataStream;

        private DataFile(string filePath)
            : this(filePath, inMemory: false)
        {
        }

        private DataFile(string filePath, bool inMemory)
        {
            this.filePath = filePath;
            this.inMemory = inMemory;
        }

        public static DataFile CreateNewPrimary(string filePath, bool inMemory = false)
        {
            static void writeInitialData(FileStream fs) { }
            var fileSize = 64 * Size.Megabyte;
            return Create(filePath, writeInitialData, fileSize, inMemory);
        }

        private static DataFile Create(string filePath, Action<FileStream> writeInitialData, int fileSize, bool inMemory)
        {
            var chunk = new DataFile(filePath, inMemory);
            try
            {
                chunk.InitNewFile(writeInitialData, fileSize);
                return chunk;
            }
            catch
            {
                chunk.Dispose();
                throw;
            }
        }

        private static DataFile OpenExisting(string filePath)
        {
            var chunk = new DataFile(filePath);
            try
            {
                chunk.InitFromExistingFile();
                return chunk;
            }
            catch
            {
                chunk.Dispose();
                throw;
            }
        }

        private void InitNewFile(Action<FileStream> writeInitialData, int fileSize)
        {
            if (this.inMemory)
            {
                throw new NotImplementedException();
            }

            // create temp file first and set desired length
            // if there is not enough disk space or something else prevents file to be resized as desired
            // we'll end up with empty temp file, which won't trigger false error on next DB verification
            var tempFilePath = $"{this.filePath}.{Guid.NewGuid()}.tmp";
            using (var tempFileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, DataStream.BufferSize, FileOptions.SequentialScan))
            {
                tempFileStream.SetLength(fileSize);

                // we need to write initial data into temp file before moving it into correct storage place, so in case of crash
                // we don't end up with seemingly valid data file with no initial data at all...
                writeInitialData(tempFileStream);

                tempFileStream.FlushToDisk();
                tempFileStream.Close();
            }

            File.Move(tempFilePath, this.filePath);

            var fileStream = new FileStream(this.filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, DataStream.BufferSize, FileOptions.SequentialScan);
            fileStream.Position = 0;

            // persist file move result
            fileStream.FlushToDisk();

            this.dataStream = new DataStream(fileStream);
        }

        private void InitFromExistingFile()
        {
            var fileInfo = new FileInfo(this.filePath);

            if (!fileInfo.Exists)
            {
                throw new InvalidOperationException($"Data file '{this.filePath}' not found.");
            }

            var fileStream = new FileStream(this.filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, DataStream.BufferSize, FileOptions.SequentialScan);
            try
            {
                // TODO: read and validate data file format.
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
            fileStream.Position = 0;

            // persist file move result
            fileStream.FlushToDisk();

            this.dataStream = new DataStream(fileStream);
        }

        public void Dispose()
        {
            if (this.dataStream != null)
            {
                this.dataStream.Dispose();
                this.dataStream = null;
            }
        }
    }
}
