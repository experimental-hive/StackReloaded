using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using StackReloaded.DataStore.StorageEngine.Pages;
using StackReloaded.DataStore.StorageEngine.Utils;
using Xunit;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Pages
{
    public class PageAccessorTests
    {       
        [Fact]
        public void StorageSizeMustBeLessOrEqualToMaximumPageStorageSize()
        {
            var maximumPageStorageSize = Page.PageSize - PageHeader.SizeOf - Page.SlotSize;

            PageAccessor.DataPageMaxStorageSize.Should().BeLessOrEqualTo(maximumPageStorageSize);
        }

        [Fact]
        public unsafe void InsertRawBytes()
        {
            byte[] b = new byte[8192];
            b.All(x => x == 0x00).Should().BeTrue();

            fixed (byte* pointer = b)
            {
                var pageAccessor = new PageAccessor();

                var p = new Page(pointer);
                p.FreeDataSize = Page.PageSize - PageHeader.SizeOf;
                p.FreeDataStart = PageHeader.SizeOf;

                p.SlotCount.Should().Be(0);
                p.FreeDataSize.Should().Be(8096);
                p.FreeDataStart.Should().Be(96);

                short clusteredKey = 1;
                byte[] recordBytes = new byte[9];
                BinaryUtil.WriteInt16(recordBytes, 0, (short)recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                recordBytes[4] = 4;
                recordBytes[5] = 5;
                recordBytes[6] = 6;
                recordBytes[7] = 7;
                recordBytes[8] = 8;

                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                void InsertRawBytes(short clusteredKey)
                {
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                    pageAccessor.InsertRawBytes(p, recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                InsertRawBytes(clusteredKey++);

                p.SlotCount.Should().Be(1);
                p.FreeDataSize.Should().Be(8085);
                p.FreeDataStart.Should().Be(105);
                BinaryUtil.ReadInt16(b, b.Length - 2).Should().Be(96);

                var i = 96;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(1);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                InsertRawBytes(clusteredKey++);

                p.SlotCount.Should().Be(2);
                p.FreeDataSize.Should().Be(8074);
                p.FreeDataStart.Should().Be(114);

                BinaryUtil.ReadInt16(b, b.Length - 2).Should().Be(96);
                BinaryUtil.ReadInt16(b, b.Length - 4).Should().Be(105);

                i = 96;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(1);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                i = 105;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(2);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                InsertRawBytes(clusteredKey++);

                p.SlotCount.Should().Be(3);
                p.FreeDataSize.Should().Be(8063);
                p.FreeDataStart.Should().Be(123);

                BinaryUtil.ReadInt16(b, b.Length - 2).Should().Be(96);
                BinaryUtil.ReadInt16(b, b.Length - 4).Should().Be(105);
                BinaryUtil.ReadInt16(b, b.Length - 6).Should().Be(114);

                i = 96;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(1);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                i = 105;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(2);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                i = 114;
                b[i + 0].Should().Be(9);
                b[i + 1].Should().Be(0);
                b[i + 2].Should().Be(3);
                b[i + 3].Should().Be(0);
                b[i + 4].Should().Be(4);
                b[i + 5].Should().Be(5);
                b[i + 6].Should().Be(6);
                b[i + 7].Should().Be(7);
                b[i + 8].Should().Be(8);

                for (int ri = 0; ri < 733; ri++)
                {
                    InsertRawBytes(clusteredKey++);
                }

                p.SlotCount.Should().Be(736);
                p.FreeDataSize.Should().Be(0);
                p.FreeDataStart.Should().Be(6720);

                BinaryUtil.ReadInt16(b, b.Length - 2).Should().Be(96);
                BinaryUtil.ReadInt16(b, b.Length - 4).Should().Be(105);
                BinaryUtil.ReadInt16(b, b.Length - 6).Should().Be(114);
                BinaryUtil.ReadInt16(b, b.Length - (736 * 2)).Should().Be(6711);

                for (int ri = 0; ri < 736; ri++)
                {
                    i = 96 + 9 * ri;

                    b[i + 0].Should().Be(9);
                    b[i + 1].Should().Be(0);
                    BinaryUtil.ReadInt16(b, i + 2).Should().Be((short)(ri + 1));
                    b[i + 4].Should().Be(4);
                    b[i + 5].Should().Be(5);
                    b[i + 6].Should().Be(6);
                    b[i + 7].Should().Be(7);
                    b[i + 8].Should().Be(8);
                }

                for (int ri = 736 - 1; ri >= 0; ri--)
                {
                    clusteredKey--;
                    pageAccessor.DeleteRawBytes(p, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);

                    p.SlotCount.Should().Be((short)ri);
                    p.FreeDataSize.Should().Be((short)(8096 - ((2 + 9) * ri)));
                    p.FreeDataStart.Should().Be((short)(96 + 9 * ri));
                }

                p.SlotCount.Should().Be(0);
                p.FreeDataSize.Should().Be(8096);
                p.FreeDataStart.Should().Be(96);
            }
        }

        [Fact]
        public unsafe void Insert10EntriesNotSortedByKeyOneByOne()
        {
            // Insert 1, 3, 5, 7, 9, 2, 4, 6, 8, 10

            // arrange
            byte[] b = new byte[8192];
            Array.Clear(b, 0, b.Length);

            fixed (byte* pointer = b)
            {
                var pageAccessor = new PageAccessor();

                var p = new Page(pointer);
                p.FreeDataSize = Page.PageSize - PageHeader.SizeOf;
                p.FreeDataStart = PageHeader.SizeOf;

                byte[] recordBytes = new byte[9];
                Array.Clear(recordBytes, 0, recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 0, (short)recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 2, 0);
                recordBytes[4] = 4;
                recordBytes[5] = 5;
                recordBytes[6] = 6;
                recordBytes[7] = 7;
                recordBytes[8] = 8;

                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    Console.WriteLine($"rBytes: {rBytes.Length}");
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                void Insert(short clusteredKey)
                {
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                    pageAccessor.InsertRawBytes(p, recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                short Slot(int index)
                {
                    return BinaryUtil.ReadInt16(p.Pointer + Page.PageSize - (index * 2) - 2);
                }

                short ClusteredKey(short slot)
                {
                    BinaryUtil.ReadInt16(p.Pointer + slot).Should().Be(9);
                    return BinaryUtil.ReadInt16(p.Pointer + slot + 2);
                }

                // act 1: Insert 1
                Insert(1);

                // assert 1
                Slot(0).Should().Be(96);
                ClusteredKey(96).Should().Be(1);
                p.FreeDataStart.Should().Be(105);

                // act 2: Insert 3
                Insert(3);

                // assert 2
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                p.FreeDataStart.Should().Be(114);

                // act 3: Insert 5
                Insert(5);

                // assert 3
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                p.FreeDataStart.Should().Be(123);

                // act 4: Insert 7
                Insert(7);

                // assert 4
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(123);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                p.FreeDataStart.Should().Be(132);

                // act 5: Insert 9
                Insert(9);

                // assert 5
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(123);
                Slot(4).Should().Be(132);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                p.FreeDataStart.Should().Be(141);

                // act 6: Insert 2
                Insert(2);

                // assert 6
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(114);
                Slot(4).Should().Be(123);
                Slot(5).Should().Be(132);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                p.FreeDataStart.Should().Be(150);

                // act 7: Insert 4
                Insert(4);

                // assert 7
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(123);
                Slot(6).Should().Be(132);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                p.FreeDataStart.Should().Be(159);

                // act 8: Insert 6
                Insert(6);

                // assert 8
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(159);
                Slot(6).Should().Be(123);
                Slot(7).Should().Be(132);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                p.FreeDataStart.Should().Be(168);

                // act 9: Insert 8
                Insert(8);

                // assert 9
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(159);
                Slot(6).Should().Be(123);
                Slot(7).Should().Be(168);
                Slot(8).Should().Be(132);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                p.FreeDataStart.Should().Be(177);

                // act 10: Insert 10
                Insert(10);

                // assert 10
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(159);
                Slot(6).Should().Be(123);
                Slot(7).Should().Be(168);
                Slot(8).Should().Be(132);
                Slot(9).Should().Be(177);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);
            }
        }

        [Fact]
        public unsafe void Delete10EntriesOneByOneInSameOrderAsAdded()
        {
            // Insert 1, 3, 5, 7, 9, 2, 4, 6, 8, 10
            // Delete 1, 3, 5, 7, 9, 2, 4, 6, 8, 10

            // arrange
            short[] keys = new short[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
            byte[] b = new byte[8192];
            Array.Clear(b, 0, b.Length);

            fixed (byte* pointer = b)
            {
                var pageAccessor = new PageAccessor();

                var p = new Page(pointer);
                p.FreeDataSize = Page.PageSize - PageHeader.SizeOf;
                p.FreeDataStart = PageHeader.SizeOf;

                byte[] recordBytes = new byte[9];
                Array.Clear(recordBytes, 0, recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 0, (short)recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 2, 0);
                recordBytes[4] = 4;
                recordBytes[5] = 5;
                recordBytes[6] = 6;
                recordBytes[7] = 7;
                recordBytes[8] = 8;

                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    Console.WriteLine($"rBytes: {rBytes.Length}");
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                void Insert(short clusteredKey)
                {
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                    pageAccessor.InsertRawBytes(p, recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                void Delete(short clusteredKey)
                {
                    pageAccessor.DeleteRawBytes(p, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                short Slot(int index)
                {
                    return BinaryUtil.ReadInt16(p.Pointer + Page.PageSize - (index * 2) - 2);
                }

                short ClusteredKey(short slot)
                {
                    BinaryUtil.ReadInt16(p.Pointer + slot).Should().Be(9);
                    return BinaryUtil.ReadInt16(p.Pointer + slot + 2);
                }

                bool Cleared(short slot)
                {
                    return BinaryUtil.AreRawBytesCleared(p.Pointer + slot, 9);
                }

                for (int i = 0; i < keys.Length; i++)
                {
                    Insert(keys[i]);
                }

                // act 1: Delete 1
                Delete(1);

                // assert 1
                Slot(0).Should().Be(141);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(150);
                Slot(3).Should().Be(114);
                Slot(4).Should().Be(159);
                Slot(5).Should().Be(123);
                Slot(6).Should().Be(168);
                Slot(7).Should().Be(132);
                Slot(8).Should().Be(177);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 2: Delete 3
                Delete(3);

                // assert 2
                Slot(0).Should().Be(141);
                Slot(1).Should().Be(150);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(159);
                Slot(4).Should().Be(123);
                Slot(5).Should().Be(168);
                Slot(6).Should().Be(132);
                Slot(7).Should().Be(177);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 3: Delete 5
                Delete(5);

                // assert 3
                Slot(0).Should().Be(141);
                Slot(1).Should().Be(150);
                Slot(2).Should().Be(159);
                Slot(3).Should().Be(123);
                Slot(4).Should().Be(168);
                Slot(5).Should().Be(132);
                Slot(6).Should().Be(177);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 4: Delete 7
                Delete(7);

                // assert 4
                Slot(0).Should().Be(141);
                Slot(1).Should().Be(150);
                Slot(2).Should().Be(159);
                Slot(3).Should().Be(168);
                Slot(4).Should().Be(132);
                Slot(5).Should().Be(177);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 5: Delete 9
                Delete(9);

                // assert 5
                Slot(0).Should().Be(141);
                Slot(1).Should().Be(150);
                Slot(2).Should().Be(159);
                Slot(3).Should().Be(168);
                Slot(4).Should().Be(177);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 6: Delete 2
                Delete(2);

                // assert 6
                Slot(0).Should().Be(150);
                Slot(1).Should().Be(159);
                Slot(2).Should().Be(168);
                Slot(3).Should().Be(177);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 7: Delete 4
                Delete(4);

                // assert 7
                Slot(0).Should().Be(159);
                Slot(1).Should().Be(168);
                Slot(2).Should().Be(177);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 8: Delete 6
                Delete(6);

                // assert 8
                Slot(0).Should().Be(168);
                Slot(1).Should().Be(177);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                ClusteredKey(168).Should().Be(8);
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 9: Delete 8
                Delete(8);

                // assert 9
                Slot(0).Should().Be(177);
                Slot(1).Should().Be(0);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                ClusteredKey(177).Should().Be(10);
                p.FreeDataStart.Should().Be(186);

                // act 10: Delete 10
                Delete(10);

                // assert 10
                Slot(0).Should().Be(0);
                Slot(1).Should().Be(0);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(96);
            }
        }

        [Fact]
        public unsafe void Delete10EntriesOneByOneInReverseOrderAsAdded()
        {
            // Insert 1, 3, 5, 7, 9, 2, 4, 6, 8, 10
            // Delete 10, 8, 6, 4, 2, 9, 7, 5, 3, 1

            // arrange
            short[] keys = new short[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
            byte[] b = new byte[8192];
            Array.Clear(b, 0, b.Length);

            fixed (byte* pointer = b)
            {
                var pageAccessor = new PageAccessor();

                var p = new Page(pointer);
                p.FreeDataSize = Page.PageSize - PageHeader.SizeOf;
                p.FreeDataStart = PageHeader.SizeOf;

                byte[] recordBytes = new byte[9];
                Array.Clear(recordBytes, 0, recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 0, (short)recordBytes.Length);
                BinaryUtil.WriteInt16(recordBytes, 2, 0);
                recordBytes[4] = 4;
                recordBytes[5] = 5;
                recordBytes[6] = 6;
                recordBytes[7] = 7;
                recordBytes[8] = 8;

                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    Console.WriteLine($"rBytes: {rBytes.Length}");
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                void Insert(short clusteredKey)
                {
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                    pageAccessor.InsertRawBytes(p, recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                void Delete(short clusteredKey)
                {
                    pageAccessor.DeleteRawBytes(p, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }

                short Slot(int index)
                {
                    return BinaryUtil.ReadInt16(p.Pointer + Page.PageSize - (index * 2) - 2);
                }

                short ClusteredKey(short slot)
                {
                    BinaryUtil.ReadInt16(p.Pointer + slot).Should().Be(9);
                    return BinaryUtil.ReadInt16(p.Pointer + slot + 2);
                }

                bool Cleared(short slot)
                {
                    return BinaryUtil.AreRawBytesCleared(p.Pointer + slot, 9);
                }

                for (int i = 0; i < keys.Length; i++)
                {
                    Insert(keys[i]);
                }

                // act 1: Delete 10
                Delete(10);

                // assert 1
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(159);
                Slot(6).Should().Be(123);
                Slot(7).Should().Be(168);
                Slot(8).Should().Be(132);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                ClusteredKey(168).Should().Be(8);
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(177);

                // act 2: Delete 8
                Delete(8);

                // assert 2
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(159);
                Slot(6).Should().Be(123);
                Slot(7).Should().Be(132);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                ClusteredKey(159).Should().Be(6);
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(168);

                // act 3: Delete 6
                Delete(6);

                // assert 3
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(150);
                Slot(4).Should().Be(114);
                Slot(5).Should().Be(123);
                Slot(6).Should().Be(132);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                ClusteredKey(150).Should().Be(4);
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(159);

                // act 4: Delete 4
                Delete(4);

                // assert 4
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(141);
                Slot(2).Should().Be(105);
                Slot(3).Should().Be(114);
                Slot(4).Should().Be(123);
                Slot(5).Should().Be(132);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                ClusteredKey(141).Should().Be(2);
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(150);

                // act 5: Delete 2
                Delete(2);

                // assert 5
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(123);
                Slot(4).Should().Be(132);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                ClusteredKey(132).Should().Be(9);
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(141);

                // act 6: Delete 9
                Delete(9);

                // assert 6
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(123);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                ClusteredKey(123).Should().Be(7);
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(132);

                // act 7: Delete 7
                Delete(7);

                // assert 7
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(114);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                ClusteredKey(114).Should().Be(5);
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(123);

                // act 8: Delete 5
                Delete(5);

                // assert 8
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(105);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                ClusteredKey(105).Should().Be(3);
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(114);

                // act 9: Delete 3
                Delete(3);

                // assert 9
                Slot(0).Should().Be(96);
                Slot(1).Should().Be(0);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                ClusteredKey(96).Should().Be(1);
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(105);

                // act 10: Delete 1
                Delete(1);

                // assert 10
                Slot(0).Should().Be(0);
                Slot(1).Should().Be(0);
                Slot(2).Should().Be(0);
                Slot(3).Should().Be(0);
                Slot(4).Should().Be(0);
                Slot(5).Should().Be(0);
                Slot(6).Should().Be(0);
                Slot(7).Should().Be(0);
                Slot(8).Should().Be(0);
                Slot(9).Should().Be(0);
                Cleared(96).Should().BeTrue();
                Cleared(105).Should().BeTrue();
                Cleared(114).Should().BeTrue();
                Cleared(123).Should().BeTrue();
                Cleared(132).Should().BeTrue();
                Cleared(141).Should().BeTrue();
                Cleared(150).Should().BeTrue();
                Cleared(159).Should().BeTrue();
                Cleared(168).Should().BeTrue();
                Cleared(177).Should().BeTrue();
                p.FreeDataStart.Should().Be(96);
            }
        }
    }
}
