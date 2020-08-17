using System.Runtime.CompilerServices;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal unsafe struct Page
    {
        public const int PageSize = 8 * Size.Kilobyte; // 16 x 512B (sector size) = 8192B = 8KB
        public const int SlotSize = 2;

        public readonly byte* Pointer;

        public Page(byte* pointer)
        {
            this.Pointer = pointer;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer != null;
        }

        public PageHeader* PageHeaderPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (PageHeader*)this.Pointer;
        }

        public int PageNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->PageId.PageNumber;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->PageId.PageNumber = value; }
        }

        public short SlotCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->SlotCount;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->SlotCount = value; }
        }

        public short FreeDataSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->FreeCount;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->FreeCount = value; }
        }

        public short FreeDataStart
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->FreeData;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->FreeData = value; }
        }
    }
}
