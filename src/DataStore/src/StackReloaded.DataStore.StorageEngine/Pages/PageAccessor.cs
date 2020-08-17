﻿using System;
using System.Buffers;
using System.Collections.Generic;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal unsafe class PageAccessor
    {
        internal const int DataPageMaxStorageSize = 8060;
        private const int RentSlotCount = 1024;

        public void InsertRawBytes<TKey>(Page page, ReadOnlySpan<byte> recordBytes, TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer)
        {
            var recordSize = recordBytes.Length;
            var maxStorageSize = DataPageMaxStorageSize;

            if (recordSize > maxStorageSize)
            {
                throw new InvalidOperationException($"Record size too large: {recordSize}.");
            }

            if (recordSize > (Page.PageSize - PageHeader.SizeOf - 2))
            {
                throw new InvalidOperationException($"Record with size {recordSize} cannot fit on this page.");
            }

            if (recordSize > (page.FreeDataSize - 2))
            {
                throw new InvalidOperationException($"Record with size {recordSize} cannot fit on this page.");
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

            var slotCount = page.SlotCount;

            if (slotCount == 0)
            {
                int slotEntry = PageHeader.SizeOf;
                byte* slotRecordPoiner = page.Pointer + slotEntry;
                BinaryUtil.WriteRawBytes(slotRecordPoiner, recordBytes);
                BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - Page.SlotSize, (short)slotEntry);

                page.SlotCount = 1;
                page.FreeDataSize = (short)(Page.PageSize - PageHeader.SizeOf - recordSize - Page.SlotSize); // this.FreeCount = (short)(StorageSize - recordLength - 2);
                page.FreeDataStart = (short)(PageHeader.SizeOf + recordSize);
            }

            var slotArrayPool = ArrayPool<short>.Shared;
            short[] slotArray = slotArrayPool.Rent(RentSlotCount);

            var recordsLayoutPool = ArrayPool<ValueTuple<short, short>>.Shared;
            ValueTuple<short, short>[] recordsLayout = recordsLayoutPool.Rent(RentSlotCount);

            if (slotCount != 0)
            {
                var freeDataSize = Page.PageSize - page.FreeDataStart - slotCount * Page.SlotSize;
                var recordDoesNotFitInFreeDataSection = recordSize > freeDataSize - Page.SlotSize;

                short freeData = page.FreeDataStart;
                int slotEntry = freeData;

                if (recordDoesNotFitInFreeDataSection)
                {
                    for (int i = 0; i < slotCount; i++)
                    {
                        short slotRecordOffset = BinaryUtil.ReadInt16(page.Pointer + Page.PageSize - (i * Page.SlotSize) - Page.SlotSize);
                        byte* slotslotRecordPoiner = page.Pointer + slotRecordOffset;
                        short slotRecordSize = BinaryUtil.ReadInt16(slotslotRecordPoiner);

                        slotArray[i] = slotRecordOffset;
                        recordsLayout[i] = ValueTuple.Create(slotRecordSize, slotRecordSize);
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
                        if (possibleSlotEntry + recordSize < nextTuple.Item1)
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

                BinaryUtil.WriteRawBytes(page.Pointer + slotEntry, recordBytes);

                int extraFreeDataUsed = recordSize;

                if (clusteredKey == null)
                {
                    BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - (slotCount * Page.SlotSize) - Page.SlotSize, (short)slotEntry);
                }
                else
                {
                    // binary search algoritm
                    // slot array entries moeten in volgorde zoals volgens clustered index
                    for (int i = 0; i < slotCount; i++)
                    {
                        slotArray[i] = BinaryUtil.ReadInt16(page.Pointer + Page.PageSize - (i * Page.SlotSize) - Page.SlotSize);
                    }

                    int insertAtSlotIndex = BinarySearchSlotIndexByClusteredKey(page, clusteredKey, clusteredKeyResolver, clusteredKeyComparer, slotCount, slotArray);

                    if (insertAtSlotIndex == -1)
                    {
                        insertAtSlotIndex = slotCount;
                    }
                    else if (insertAtSlotIndex < slotCount)
                    {
                        for (int i = slotCount - 1; i >= insertAtSlotIndex; i--)
                        {
                            var j = i + 1;
                            slotArray[j] = slotArray[i];
                            BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - (j * Page.SlotSize) - Page.SlotSize, slotArray[j]);
                        }
                    }

                    slotArray[insertAtSlotIndex] = (short)slotEntry;
                    BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - (insertAtSlotIndex * Page.SlotSize) - Page.SlotSize, slotArray[insertAtSlotIndex]);
                }

                page.SlotCount = (short)(slotCount + 1);
                page.FreeDataSize -= (short)(recordSize + Page.SlotSize);
                page.FreeDataStart = (short)(freeData + extraFreeDataUsed);
            }

            recordsLayoutPool.Return(recordsLayout);
            slotArrayPool.Return(slotArray);
        }

        public void DeleteRawBytes<TKey>(Page page, TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer)
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

            var slotCount = page.SlotCount;

            if (slotCount == 0)
            {
                return;
            }

            var slotArrayPool = ArrayPool<short>.Shared;
            short[] slotArray = slotArrayPool.Rent(RentSlotCount);
            var recordsLayoutPool = ArrayPool<ValueTuple<short, short>>.Shared;
            ValueTuple<short, short>[] recordsLayout = recordsLayoutPool.Rent(RentSlotCount);

            short freeDataStart = page.FreeDataStart;

            if (clusteredKey == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                for (int i = 0; i < slotCount; i++)
                {
                    slotArray[i] = BinaryUtil.ReadInt16(page.Pointer + Page.PageSize - (i * Page.SlotSize) - Page.SlotSize);
                }

                int deleteAtSlotIndex = BinarySearchSlotIndexByClusteredKey(page, clusteredKey, clusteredKeyResolver, clusteredKeyComparer, slotCount, slotArray, skipClusteredKeyAlreadyExist: true);

                if (deleteAtSlotIndex < 0 || deleteAtSlotIndex >= slotCount)
                {
                    if (deleteAtSlotIndex == -1)
                    {
                        throw new NotImplementedException();
                    }

                    throw new InvalidOperationException();
                }

                short recordOffset = slotArray[deleteAtSlotIndex];
                byte* recordPoiner = page.Pointer + recordOffset;
                short recordSize = BinaryUtil.ReadInt16(recordPoiner);

                if (deleteAtSlotIndex < slotCount - 1)
                {
                    for (int i = deleteAtSlotIndex; i < slotCount; i++)
                    {
                        slotArray[i] = slotArray[i + 1];
                        BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - (i * Page.SlotSize) - Page.SlotSize, slotArray[i]);
                    }
                }

                slotArray[slotCount - 1] = 0;
                BinaryUtil.WriteInt16(page.Pointer + Page.PageSize - ((slotCount - 1) * Page.SlotSize) - Page.SlotSize, slotArray[slotCount - 1]);

                BinaryUtil.ClearRawBytes(recordPoiner, recordSize);

                page.SlotCount = (short)(slotCount - 1);
                page.FreeDataSize += (short)(recordSize + Page.SlotSize);

                if (recordOffset + recordSize == freeDataStart)
                {
                    page.FreeDataStart = DetermineFreeDataStart(page, slotArray);
                }
            }

            recordsLayoutPool.Return(recordsLayout);
            slotArrayPool.Return(slotArray);
        }

        private static short DetermineFreeDataStart(Page page, short[] slotArray)
        {
            short slotCount = page.SlotCount;
            int freeData = PageHeader.SizeOf;

            for (int i = 0; i < slotCount; i++)
            {
                short recordOffset = slotArray[i];

                if (recordOffset < freeData)
                {
                    continue;
                }

                byte* recordPoiner = page.Pointer + recordOffset;
                short recordSize = BinaryUtil.ReadInt16(recordPoiner);
                int slotRecordEnd = recordOffset + recordSize;

                if (slotRecordEnd > freeData)
                {
                    freeData = slotRecordEnd;
                }
            }

            return (short)freeData;
        }

        private static int BinarySearchSlotIndexByClusteredKey<TKey>(Page page, TKey clusteredKey, ClusteredKeyResolverDelegate<TKey> clusteredKeyResolver, IComparer<TKey> clusteredKeyComparer, short slotCount, short[] slotArray, bool skipClusteredKeyAlreadyExist = false)
        {
            int low = 0, high = slotCount - 1, mid = -1;

            while (low <= high)
            {
                mid = (low + high) / 2;

                short slotRecordOffset = slotArray[mid];
                byte* slotRecordPoiner = page.Pointer + slotRecordOffset;
                int slotRecordSize = BinaryUtil.ReadInt16(slotRecordPoiner);
                int slotRecordEnd = slotRecordOffset + slotRecordSize;

                if (slotRecordSize <= 0)
                {
                    if (slotRecordSize == 0)
                    {
                        throw new InvalidOperationException($"Record size cannot be zero.");
                    }

                    throw new InvalidOperationException($"Invalid record size.");
                }

                if (slotRecordEnd > page.FreeDataStart)
                {
                    throw new InvalidOperationException($"Invalid record.");
                }

                ReadOnlySpan<byte> recordData = new ReadOnlySpan<byte>(slotRecordPoiner, slotRecordSize);
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

                if (high == low)
                {
                    if (compareResult < 0)
                    {
                        return mid;
                    }
                    else
                    {
                        return mid + 1;
                    }
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
    }
}
