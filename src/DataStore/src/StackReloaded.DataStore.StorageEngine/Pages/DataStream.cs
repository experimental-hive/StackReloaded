using System;
using System.IO;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal sealed class DataStream : IDisposable
    {
        public const int BufferSize = 8192;

        private readonly FileStream fileStream;
        private readonly MemoryStream buffer;
        private readonly BinaryWriter bufferWriter;

        private readonly UnmanagedMemoryStream memoryStream;
        private readonly Stream workingStream;

        public DataStream(FileStream fileStream)
            : this(fileStream, null)
        {
        }

        public DataStream(FileStream fileStream, UnmanagedMemoryStream memoryStream)
        {
            this.fileStream = fileStream;
            this.memoryStream = memoryStream;
            this.workingStream = (Stream)fileStream ?? memoryStream;
            this.buffer = new MemoryStream(BufferSize);
            this.bufferWriter = new BinaryWriter(this.buffer);
        }

        public Stream WorkingStream => this.workingStream;

        public long StreamLength => this.workingStream.Length;

        public long StreamPosition => this.workingStream.Position;

        public MemoryStream Buffer => this.buffer;

        public BinaryWriter BufferWriter => this.bufferWriter;

        //public int Write(Action<BinaryWriter> writeTo)
        //{
        //    // The first 4 bytes will be later overwritten as the length of the written data.
        //    this.buffer.SetLength(4);
        //    this.buffer.Position = 4;
        //    writeTo(this.bufferWriter);
        //    var length = (int)this.buffer.Length - 4;
        //    this.bufferWriter.Write(length); // length suffix
        //    this.buffer.Position = 0;
        //    this.bufferWriter.Write(length); // length prefix

        //    return length;
        //}

        //public void WriteRawData()
        //{
        //    var length = (int)this.buffer.Length;
        //    var buffer = this.buffer.GetBuffer();
        //    WriteRawData(buffer, length);
        //}

        //private void WriteRawData(byte[] buffer, int length)
        //{
        //    AppendData(buffer, length);
        //}

        //private void AppendData(byte[] buffer, int length)
        //{
        //    // as we are always append-only, stream's position should be right here
        //    if (this.fileStream != null)
        //    {
        //        this.fileStream.Write(buffer, 0, length);
        //    }

        //    //MEMORY
        //    var memoryStream = this.memoryStream;
        //    if (memoryStream != null)
        //    {
        //        memoryStream.Write(buffer, 0, length);
        //    }
        //}

        public void FlushToDisk()
        {
            this.fileStream?.FlushToDisk();
        }

        public void Dispose()
        {
            this.fileStream?.Dispose();
            this.buffer?.Dispose();
            this.bufferWriter?.Dispose();
            this.memoryStream?.Dispose();
        }
    }
}
