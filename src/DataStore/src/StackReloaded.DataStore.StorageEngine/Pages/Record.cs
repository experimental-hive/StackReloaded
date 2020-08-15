using System.Runtime.CompilerServices;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal unsafe struct Record
    {
        public readonly byte* Pointer;
        public readonly byte* PagePointer;

        public Record(byte* pointer, byte* pagePointer)
        {
            this.Pointer = pointer;
            this.PagePointer = pagePointer;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer != null;
        }

        /// <summary>
        /// Status bits from first byte.
        /// </summary>
        public byte StatusBits1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer[0];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Pointer[0] = value;
        }

        /// <summary>
        /// Status bits from second byte.
        /// </summary>
        public byte StatusBits2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer[1];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Pointer[1] = value;
        }

        /// <summary>
        /// Length of fixed length portion of record.
        /// </summary>
        public short FixedLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BinaryUtil.ReadInt16(this.Pointer + 2);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => BinaryUtil.WriteInt16(this.Pointer + 2, value);
        }

        /// <summary>
        /// Length of fixed length data.
        /// </summary>
        public short FixedLengthDataLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)(this.FixedLength - 4);
        }

        /// <summary>
        /// The pointer to the fixed length data.
        /// </summary>
        public byte* FixedLengthData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer + 4;
        }

        /// <summary>
        /// The total number of columns in the records.
        /// </summary>
        public short NumberOfColumns
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BinaryUtil.ReadInt16(this.FixedLengthData + this.FixedLengthDataLength);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => BinaryUtil.WriteInt16(this.FixedLengthData + this.FixedLengthDataLength, value);
        }

        /// <summary>
        /// The length of the NULL bitmap based on number of columns.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetNullBitmapLength(short numberOfColumns)
        {
            return (short)((numberOfColumns + 7) / 8);
        }

        /// <summary>
        /// NULL bitmap: 1 bit for each column.
        /// </summary>
        public byte* NullBitmapData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.FixedLengthData + this.FixedLengthDataLength + 2;
        }

        /// <summary>
        /// Number of variable length columns.
        /// </summary>
        public short NumberOfVariableColumns
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BinaryUtil.ReadInt16(this.FixedLengthData + this.FixedLengthDataLength + 2 + GetNullBitmapLength(this.NumberOfColumns));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => BinaryUtil.WriteInt16(this.FixedLengthData + this.FixedLengthDataLength + 2 + GetNullBitmapLength(this.NumberOfColumns), value);
        }

        /// <summary>
        /// Variable column offset array data.
        /// </summary>
        public byte* VariableColumnOffsetArrayData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.FixedLengthData + this.FixedLengthDataLength + 2 + GetNullBitmapLength(this.NumberOfColumns) + 2;
        }

        /// <summary>
        /// Variable columns data.
        /// </summary>
        public byte* VariableColumnsData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.FixedLengthData + this.FixedLengthDataLength + 2 + GetNullBitmapLength(this.NumberOfColumns) + 2 + (2 * this.NumberOfVariableColumns);
        }
    }
}
