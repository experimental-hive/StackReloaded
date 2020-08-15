using System.Runtime.InteropServices;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeOf)]
    internal unsafe struct PageHeader
    {
        public const int SizeOf = 96;

        /// <summary>
        /// This is the page header version. Since version 1.0 this value has always been 1.
        /// </summary>
        [FieldOffset(0)]
        public byte HeaderVersion;

        /// <summary>
        /// This is the page type.
        /// </summary>
        [FieldOffset(1)]
        public PageType Type;

        /// <summary>
        /// This stores a few values about the page. For data and index pages, if the field is 4, that means all the rows on the page are the same fixed size.
        /// </summary>
        [FieldOffset(2)]
        public byte TypeFlagBits;

        /// <summary>
        /// This is the level that the page is part of in the b-tree.
        /// Levels are numbered from 0 at the leaf-level and increase to the single-page root level (i.e.the top of the b-tree).
        /// For all page types apart from index pages, the level is always 0.
        /// </summary>
        [FieldOffset(3)]
        public byte Level;

        /// <summary>
        /// This identifies the file number the page is part of and the position within the file.
        /// </summary>
        [FieldOffset(4)]
        public PagePointer PageId;

        /// <summary>
        /// These are pointers to the previous and next pages at this level of the b-tree and store 6-byte page IDs.
        /// The pages in each level of an index are joined in a doubly-linked list according to the logical order(as defined by the index keys) of the index. The pointers do not necessarily point to the immediately adjacent physical pages in the file (because of fragmentation).
        /// The pages on the left-hand side of a b-tree level will have the PrevPage pointer be NULL, and those on the right-hand side will have the NextPage be NULL.
        /// In a heap, or if an index only has a single page, these pointers will both be NULL for all pages.
        /// </summary>
        [FieldOffset(10)]
        public PagePointer PrevPage;

        /// <summary>
        /// These are pointers to the previous and next pages at this level of the b-tree and store 6-byte page IDs.
        /// The pages in each level of an index are joined in a doubly-linked list according to the logical order(as defined by the index keys) of the index. The pointers do not necessarily point to the immediately adjacent physical pages in the file (because of fragmentation).
        /// The pages on the left-hand side of a b-tree level will have the PrevPage pointer be NULL, and those on the right-hand side will have the NextPage be NULL.
        /// In a heap, or if an index only has a single page, these pointers will both be NULL for all pages.
        /// </summary>
        [FieldOffset(16)]
        public PagePointer NextPage;

        /// <summary>
        /// This is the size of the fixed-length portion of the records on the page.
        /// </summary>
        /// <remarks>
        /// Index records don't contain fixed length header, it's stored in the page header.
        /// </remarks>
        [FieldOffset(22)]
        public short RecordFixedLength; // aka pminlen

        /// <summary>
        /// This is the count of records on the page.
        /// </summary>
        [FieldOffset(24)]
        public short SlotCount;

        /// <summary>
        /// This is the number of bytes of free space in the page.
        /// </summary>
        [FieldOffset(26)]
        public short FreeCount;

        /// <summary>
        /// This is the offset from the start of the page to the first byte after the end of the last record on the page. It doesn't matter if there is free space nearer to the start of the page.
        /// </summary>
        [FieldOffset(28)]
        public short FreeData;

        [FieldOffset(30)]
        public fixed byte Unused[66];
    }
}
