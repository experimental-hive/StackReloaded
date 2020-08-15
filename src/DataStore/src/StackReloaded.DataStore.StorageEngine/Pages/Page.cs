using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal unsafe struct Page
    {
        public const int PageSize = 8 * Size.Kilobyte;
        public const int StorageSize = 8060;

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

#pragma warning disable CA1822 // Mark members as static
        public int RawSize
#pragma warning restore CA1822 // Mark members as static
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PageSize;
        }

        public PageHeader* PageHeaderPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (PageHeader*)this.Pointer;
        }

        public byte* DataPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Pointer + PageHeader.SizeOf;
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

        public short FreeCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->FreeCount;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->FreeCount = value; }
        }

        public short FreeData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((PageHeader*)this.Pointer)->FreeData;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { ((PageHeader*)this.Pointer)->FreeData = value; }
        }

        public void InsertRawBytes<TKey>(ReadOnlySpan<byte> bytes, TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer)
        {
            var byteLength = bytes.Length;

            if (byteLength > StorageSize)
            {
                throw new InvalidOperationException($"Record size too large: {byteLength}.");
            }

            var storedByteLength = BinaryUtil.ReadInt16(bytes);

            if (storedByteLength != byteLength)
            {
                throw new InvalidOperationException($"Record with size {byteLength} has different stored size: {storedByteLength}.");
            }

            if (byteLength > (this.RawSize - PageHeader.SizeOf - 2))
            {
                throw new InvalidOperationException($"Bytes with byte size {byteLength} cannot fit on this page.");
            }

            if (byteLength > (this.FreeCount - 2))
            {
                throw new InvalidOperationException($"Bytes with byte size {byteLength} cannot fit on this page.");
            }

            if (clusteredKey != null)
            {
                if (clusteredKeyResolver == null)
                {
                    throw new ArgumentNullException(nameof(clusteredKeyResolver));
                }

                if (clusteredKeyComparer == null)
                {
                    throw new ArgumentNullException(nameof(clusteredKeyComparer));
                }
            }

            var slotCount = this.SlotCount;

            var slotArrayPool = ArrayPool<short>.Shared;
            short[] slotArray = slotArrayPool.Rent(slotCount + 1);

            var recordsLayoutPool = ArrayPool<ValueTuple<short, short>>.Shared;
            ValueTuple<short, short>[] recordsLayout = recordsLayoutPool.Rent(slotCount);

            if (slotCount != 0)
            {
                var freeDataSectionSize = this.RawSize - this.FreeData - slotCount * 2;
                var recordDoesNotFitInFreeDataSection = byteLength > freeDataSectionSize - 2;

                short freeData = this.FreeData;
                int slotEntry = freeData;

                if (recordDoesNotFitInFreeDataSection)
                {
                    for (int i = 0; i < slotCount; i++)
                    {
                        short recordOffset = BinaryUtil.ReadInt16(this.Pointer + this.RawSize - (i * 2) - 2);
                        byte* dataRecordPointer = this.Pointer + recordOffset;
                        short recordSize = BinaryUtil.ReadInt16(dataRecordPointer);

                        slotArray[i] = recordOffset;
                        recordsLayout[i] = ValueTuple.Create(recordOffset, recordSize);
                    }

                    // TODO Optimize using System.Array.Sort(...)
                    // var orderedRecordsLayout = recordsLayout.OrderBy(x => x.Item1).ToList();

                    // TODO Check/Validating records layout...

                    // Proberen gaten op te vullen: zoek naar een slot entry.
                    bool slotEntryFound = false;
                    for (int i = 0; i < slotCount - 1; i++)
                    {
                        var tuple = recordsLayout[i];
                        var nextTuple = recordsLayout[i + 1];
                        var possibleSlotEntry = tuple.Item1 + tuple.Item2;
                        if (possibleSlotEntry + byteLength < nextTuple.Item1)
                        {
                            slotEntry = (short)possibleSlotEntry;
                            slotEntryFound = true;
                            break;
                        }
                    }

                    if (!slotEntryFound)
                    {
                        // TODO
                        throw new NotImplementedException();
                    }
                }

                BinaryUtil.WriteRawBytes(this.Pointer + slotEntry, bytes);

                bool slotInserted = false;

                if (clusteredKey == null)
                {
                    BinaryUtil.WriteInt16(this.Pointer + this.RawSize - (slotCount * 2) - 2, (short)slotEntry);
                }
                else
                {
                    // binary search algoritm
                    // slot array entries moeten in volgorde zoals volgens clustered index
                    for (int i = 0; i < slotCount; i++)
                    {
                        slotArray[i] = BinaryUtil.ReadInt16(this.Pointer + this.RawSize - (i * 2) - 2);
                    }

                    int insertAtSlotIndex = BinarySearchSlotIndexByClusteredKey(clusteredKey, clusteredKeyResolver, clusteredKeyComparer, slotCount, slotArray);

                    if (insertAtSlotIndex >= 0 && insertAtSlotIndex < slotCount - 1)
                    {
                        for (int j = insertAtSlotIndex + 1; j < slotCount; j++)
                        {
                            slotArray[j] = slotArray[j - 1];
                            BinaryUtil.WriteInt16(this.Pointer + this.RawSize - (j * 2) - 2, slotArray[j]);
                        }

                        slotArray[insertAtSlotIndex] = (short)slotEntry;
                        BinaryUtil.WriteInt16(this.Pointer + this.RawSize - (insertAtSlotIndex * 2) - 2, slotArray[insertAtSlotIndex]);

                        slotInserted = true;
                    }

                    if (!slotInserted)
                    {
                        slotArray[slotCount] = (short)slotEntry;
                        BinaryUtil.WriteInt16(this.Pointer + this.RawSize - (slotCount * 2) - 2, slotArray[slotCount]);
                    }
                }

                this.SlotCount = (short)(slotCount + 1);
                this.FreeCount -= (short)(byteLength + 2);

                if (!slotInserted)
                {
                    this.FreeData = (short)(freeData + byteLength);
                }
            }
            else
            {
                int slotEntry = PageHeader.SizeOf;
                byte* dataRecordPointer = this.Pointer + slotEntry;
                BinaryUtil.WriteRawBytes(dataRecordPointer, bytes);
                BinaryUtil.WriteInt16(this.Pointer + this.RawSize - 2, (short)slotEntry);

                this.SlotCount = 1;
                this.FreeCount = (short)(this.RawSize - PageHeader.SizeOf - byteLength - 2); // this.FreeCount = (short)(StorageSize - byteLength - 2);
                this.FreeData = (short)(PageHeader.SizeOf + byteLength);
            }

            recordsLayoutPool.Return(recordsLayout);
            slotArrayPool.Return(slotArray);
        }

        public void DeleteRawBytes<K>(K clusteredKey, ClusteredKeyResolverDelegate<K> clusteredKeyResolver, IComparer<K> clusteredKeyComparer)
        {
            if (clusteredKey != null)
            {
                if (clusteredKeyResolver == null)
                {
                    throw new ArgumentNullException(nameof(clusteredKeyResolver));
                }

                if (clusteredKeyComparer == null)
                {
                    throw new ArgumentNullException(nameof(clusteredKeyComparer));
                }
            }

            var slotCount = this.SlotCount;

            if (slotCount == 0)
            {
                return;
            }

            var slotArrayPool = ArrayPool<short>.Shared;
            short[] slotArray = slotArrayPool.Rent(slotCount);

            var recordsLayoutPool = ArrayPool<ValueTuple<short, short>>.Shared;
            ValueTuple<short, short>[] recordsLayout = recordsLayoutPool.Rent(slotCount);

            short freeData = this.FreeData;

            if (clusteredKey == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                // binary search algoritm
                // slot array entries moeten in volgorde zoals volgens clustered index
                for (int i = 0; i < slotCount; i++)
                {
                    slotArray[i] = BinaryUtil.ReadInt16(this.Pointer + this.RawSize - (i * 2) - 2);
                }

                int deleteAtSlotIndex = BinarySearchSlotIndexByClusteredKey(clusteredKey, clusteredKeyResolver, clusteredKeyComparer, slotCount, slotArray, skipClusteredKeyAlreadyExist: true);

                if (deleteAtSlotIndex >= 0 && deleteAtSlotIndex < slotCount)
                {
                    short recordOffset = slotArray[deleteAtSlotIndex];
                    byte* dataRecordPointer = this.Pointer + recordOffset;
                    short recordSize = BinaryUtil.ReadInt16(dataRecordPointer);
                    int byteLength = recordSize;

                    if (deleteAtSlotIndex < slotCount - 1)
                    {
                        for (int j = deleteAtSlotIndex + 1; j < slotCount - 1; j++)
                        {
                            slotArray[j - 1] = slotArray[j];
                            BinaryUtil.WriteInt16(this.Pointer + this.RawSize - ((j - 1) * 2) - 2, slotArray[j - 1]);
                        }
                    }

                    slotArray[slotCount - 1] = (short)0;
                    BinaryUtil.WriteInt16(this.Pointer + this.RawSize - ((slotCount - 1) * 2) - 2, slotArray[slotCount - 1]);

                    BinaryUtil.ClearRawBytes(dataRecordPointer, byteLength);

                    this.SlotCount = (short)(slotCount - 1);
                    this.FreeCount += (short)(byteLength + 2);

                    if (recordOffset + byteLength == freeData)
                    {
                        this.FreeData = (short)(freeData - byteLength);
                    }
                }
            }

            recordsLayoutPool.Return(recordsLayout);
            slotArrayPool.Return(slotArray);
        }

        private int BinarySearchSlotIndexByClusteredKey<TKey>(TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer, short slotCount, short[] slotArray, bool skipClusteredKeyAlreadyExist = false)
        {
            int low = 0, high = slotCount - 1, mid = -1;

            while (low <= high)
            {
                mid = (low + high) / 2;

                byte* dataRecordPointer = this.Pointer + slotArray[mid];
                int recordSize = BinaryUtil.ReadInt16(dataRecordPointer);
                if (recordSize > StorageSize)
                {
                    throw new InvalidOperationException($"Record size too large: {recordSize}");
                }

                ReadOnlySpan<byte> recordData = new ReadOnlySpan<byte>(dataRecordPointer, recordSize);
                TKey recordClusteredKey = clusteredKeyResolver(recordData);

                int compareResult = clusteredKeyComparer.Compare(clusteredKey, recordClusteredKey);

                if (compareResult == 0)
                {
                    if (skipClusteredKeyAlreadyExist)
                    {
                        return mid;
                    }

                    throw new InvalidOperationException($"Clustered key {clusteredKey} already exist in this page.");
                }

                if (compareResult < 0)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return mid;
        }

        private int BinarySearchLastIndexNotTestedWell<TKey>(TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer, short slotCount, short[] slotArray)
        {
            int low = 0, high = slotCount - 1, mid = -1;

            while (low <= high)
            {
                mid = (low + high) / 2;

                byte* dataRecordPointer = this.Pointer + slotArray[mid];
                int recordSize = BinaryUtil.ReadInt16(dataRecordPointer);
                if (recordSize > StorageSize)
                {
                    throw new InvalidOperationException($"Record size too large: {recordSize}");
                }

                ReadOnlySpan<byte> recordData = new ReadOnlySpan<byte>(dataRecordPointer, recordSize);
                TKey recordClusteredKey = clusteredKeyResolver(recordData);

                int comp = clusteredKeyComparer.Compare(clusteredKey, recordClusteredKey);

                if (comp <= -1)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return mid;
        }
    }
}
