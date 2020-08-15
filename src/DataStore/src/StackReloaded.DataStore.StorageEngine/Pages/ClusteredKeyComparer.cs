using System;
using StackReloaded.DataStore.StorageEngine.Utils;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal class ClusteredKeyComparer
    {
        public enum ComparandType : byte
        {
            None = 0,
            Short = 1
        }

        private readonly ComparandType[] args;

        public ClusteredKeyComparer(params ComparandType[] args)
        {
            this.args = args;
        }

        public unsafe int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            fixed (byte* pA = &a[0])
            fixed (byte* pB = &b[0])
            {
                int c = 0;

                for (int i = 0; i < this.args.Length; i++)
                {
                    c = Compare(this.args[i], pA + (i << 1), pB + (i << 1));

                    if (c != 0)
                    {
                        return c;
                    }
                }

                return c;
            }
        }

        private static unsafe int Compare(ComparandType t, byte* pValueA, byte* pValueB)
        {
            switch (t)
            {
                case ComparandType.Short:
                    return Compare(BinaryUtil.ReadInt16(pValueA), BinaryUtil.ReadInt16(pValueB));

            }

            throw new InvalidOperationException();
        }

        private static int Compare(short a, short b)
        {
            return a.CompareTo(b);
        }
    }
}
