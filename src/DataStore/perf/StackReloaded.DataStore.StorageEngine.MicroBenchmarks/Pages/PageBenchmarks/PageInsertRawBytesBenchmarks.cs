using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using StackReloaded.DataStore.StorageEngine.Pages;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks.Pages.PageBenchmarks
{
    public class PageInsertRawBytesBenchmarks
    {
        private short[] keys;
        private byte[] b;
        private byte[] recordOf9Bytes;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.keys = new short[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
            this.b = new byte[8192];
            this.recordOf9Bytes = new byte[9];
        }

        [Benchmark]
        public unsafe void InsertRawBytes()
        {
            short[] keys = this.keys;
            byte[] b = this.b;
            Array.Clear(b, 0, b.Length);

            fixed (byte* pointer = b)
            {
                var p = new Page(pointer);
                p.FreeCount = Page.PageSize - PageHeader.SizeOf;
                p.FreeData = PageHeader.SizeOf;

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
                    p.InsertRawBytes(recordBytes, clusteredKey, clusteredKeyResolver, clusteredKeyComparer);
                }
            }
        }
    }
}
