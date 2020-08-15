using System;
using System.Runtime.InteropServices;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal struct PagePointer : IEquatable<PagePointer>
    {
        public const int SizeOf = 6;

        [FieldOffset(0)]
        public int PageNumber;

        [FieldOffset(4)]
        public short FileId;

        public static readonly PagePointer Zero = new PagePointer(0, 0);

        public PagePointer(short fileId, int pageNumber)
        {
            this.FileId = fileId;
            this.PageNumber = pageNumber;
        }

        public PagePointer(byte[] bytes)
        {
            if (bytes.Length != 6)
            {
                throw new ArgumentException("Input must be 6 bytes in the format PageNumber(4)FileId(2).");
            }

            this.PageNumber = BitConverter.ToInt32(bytes, 0);
            this.FileId = BitConverter.ToInt16(bytes, 4);
        }

        public PagePointer(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 6)
            {
                throw new ArgumentException("Input must be 6 bytes in the format PageNumber(4)FileId(2).");
            }

            this.PageNumber = BitConverter.ToInt32(bytes.Slice(0, 4));
            this.FileId = BitConverter.ToInt16(bytes.Slice(4, 2));
        }

        public static bool operator ==(PagePointer a, PagePointer b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PagePointer a, PagePointer b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            return Equals((PagePointer)obj);
        }

        public bool Equals(PagePointer other)
        {
            return other.FileId == this.FileId && other.PageNumber == this.PageNumber;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h1 = this.FileId.GetHashCode();
                var h2 = this.PageNumber.GetHashCode();

                // Code copied from System.Tuple so it must be the best way to combine hash codes or at least a good one.
                return ((h1 << 5) + h1) ^ h2;
            }
        }

        public override string ToString()
        {
            return $"({this.FileId}:{this.PageNumber})";
        }
    }
}
