using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using StackReloaded.DataStore.StorageEngine.Pages;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks.Pages.PageBenchmarks
{
    public class PageDeleteRawBytesBenchmarks
    {
        private byte[] b;
        private byte[] recordOf9Bytes;
        private short clusteredKey;

        [GlobalSetup]
        public unsafe void GlobalSetup()
        {
            this.b = new byte[8192];
            this.recordOf9Bytes = new byte[9];
        }

        [IterationSetup]
        public unsafe void IterationSetup()
        {
            byte[] b = this.b;
            short clusteredKey = 1;

            Array.Clear(b, 0, b.Length);

            fixed (byte* pointer = b)
            {
                var p = new Page(pointer);
                p.FreeCount = Page.PageSize - PageHeader.SizeOf;
                p.FreeData = PageHeader.SizeOf;

                byte[] recordBytes = this.recordOf9Bytes;
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

                for (int ri = 0; ri < 736; ri++)
                {
                    BinaryUtil.WriteInt16(recordBytes, 2, clusteredKey++);
                    p.InsertRawBytes(recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }
            }

            this.clusteredKey = clusteredKey;
        }

        [Benchmark]
        public unsafe void DeleteRawBytes()
        {
            byte[] b = this.b;
            short clusteredKey = this.clusteredKey;

            fixed (byte* pointer = b)
            {
                var p = new Page(pointer);
                ClusteredKeyResolverDelegate<short> clusteredKeyResolver = (rBytes) =>
                {
                    var rKey = BinaryUtil.ReadInt16(rBytes.Slice(2));
                    return rKey;
                };

                IComparer<short> clusteredKeyComparer = Comparer<short>.Default;

                for (int ri = 736 - 1; ri >= 0; ri--)
                {
                    clusteredKey--;
                    p.DeleteRawBytes(clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }
            }
        }
    }
}
