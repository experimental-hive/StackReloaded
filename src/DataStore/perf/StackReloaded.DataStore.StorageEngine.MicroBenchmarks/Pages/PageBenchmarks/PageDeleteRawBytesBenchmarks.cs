using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using StackReloaded.DataStore.StorageEngine.Pages;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks.Pages.PageBenchmarks
{
    public class PageDeleteRawBytesBenchmarks
    {
        private short[] keys;
        private byte[] b;
        private byte[] recordOf9Bytes;
        private PageAccessor pageAccessor;

        [GlobalSetup]
        public unsafe void GlobalSetup()
        {
            this.keys = new short[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
            this.b = new byte[8192];
            this.recordOf9Bytes = new byte[9];
            this.pageAccessor = new PageAccessor();
        }

        [IterationSetup]
        public unsafe void IterationSetup()
        {
            short[] keys = this.keys;
            byte[] b = this.b;
            Array.Clear(b, 0, b.Length);
            var pageAccessor = this.pageAccessor;

            fixed (byte* pointer = b)
            {
                var p = new Page(pointer);
                p.FreeDataSize = Page.PageSize - PageHeader.SizeOf;
                p.FreeDataStart = PageHeader.SizeOf;

                byte[] recordBytes = this.recordOf9Bytes;
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
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                for (int i = 0; i < keys.Length; i++)
                {
                    var clusteredKey = keys[i];
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey);
                    pageAccessor.InsertRawBytes(p, recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }
            }
        }

        [Benchmark]
        public unsafe void DeleteRawBytes()
        {
            short[] keys = this.keys;
            byte[] b = this.b;
            var pageAccessor = this.pageAccessor;

            fixed (byte* pointer = b)
            {
                var p = new Page(pointer);
                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                for (int i = 0; i < keys.Length; i++)
                {
                    var clusteredKey = keys[i];
                    pageAccessor.DeleteRawBytes(p, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }
            }
        }
    }
}
