using System;
using System.Runtime.CompilerServices;

namespace StackReloaded.DataStore.StorageEngine.Utils
{
    internal static class BinaryUtil
    {
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long ReadInt64(byte* rawBytes)
        {
            var val = *(long*)rawBytes;

            if (!IsLittleEndian)
            {
                return SwapBitShift(val);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ReadInt32(byte* rawBytes)
        {
            var val = *(int*)rawBytes;

            if (!IsLittleEndian)
            {
                return SwapBitShift(val);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ReadInt16(byte* rawBytes)
        {
            var val = *(short*)rawBytes;

            if (!IsLittleEndian)
            {
                return SwapBitShift(val);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ReadInt16(ReadOnlySpan<byte> span)
        {
            fixed (byte* rawBytes = &span[0])
            {
                var val = *(short*)rawBytes;

                if (!IsLittleEndian)
                {
                    return SwapBitShift(val);
                }

                return val;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ReadInt16(Span<byte> span)
        {
            fixed (byte* rawBytes = &span[0])
            {
                var val = *(short*)rawBytes;

                if (!IsLittleEndian)
                {
                    return SwapBitShift(val);
                }

                return val;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ReadInt16(Memory<byte> memory)
        {
            fixed (byte* rawBytes = &memory.Span[0])
            {
                var val = *(short*)rawBytes;

                if (!IsLittleEndian)
                {
                    return SwapBitShift(val);
                }

                return val;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ReadInt16(byte[] rawBytes, int startIndex)
        {
            var byte01 = (uint)rawBytes[startIndex];
            var byte02 = (uint)rawBytes[startIndex + 1];
            var val = (short)(((byte02 << 8) & 0x0000FF00) | byte01);

            if (!IsLittleEndian)
            {
                return SwapBitShift(val);
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadRawBytes(byte* rawBytes, byte[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[i] = rawBytes[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReadRawBytes(byte* rawBytes, byte[] buffer, int startIndex, int count)
        {
            for (int i = 0, vi = startIndex; i < count; i++, vi++)
            {
                buffer[vi] = rawBytes[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt64(byte* rawBytes, long val)
        {
            if (!IsLittleEndian)
            {
                *(long*)rawBytes = SwapBitShift(val);
            }

            *(long*)rawBytes = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt32(byte* rawBytes, int val)
        {
            if (!IsLittleEndian)
            {
                *(int*)rawBytes = SwapBitShift(val);
            }

            *(int*)rawBytes = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt16(byte* rawBytes, short val)
        {
            if (!IsLittleEndian)
            {
                *(short*)rawBytes = SwapBitShift(val);
            }

            *(short*)rawBytes = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt16(byte[] rawBytes, int startIndex, short val)
        {
            if (!IsLittleEndian)
            {
                val = SwapBitShift(val);
            }

            uint uvalue = (uint)val;

            rawBytes[startIndex] = (byte)(uvalue & 0x000000FF);
            rawBytes[startIndex + 1] = (byte)((uvalue >> 8) & 0x000000FF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRawBytes(byte* rawBytes, byte[] val, int count)
        {
            for (int i = 0; i < count; i++)
            {
                rawBytes[i] = val[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRawBytes(byte* rawBytes, ReadOnlySpan<byte> val)
        {
            for (int i = 0; i < val.Length; i++)
            {
                rawBytes[i] = val[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRawBytes(byte* rawBytes, byte* val, int count)
        {
            for (int i = 0; i < count; i++)
            {
                rawBytes[i] = val[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteRawBytes(byte* rawBytes, byte[] val, int startIndex, int count)
        {
            for (int i = 0, vi = startIndex; i < count; i++, vi++)
            {
                rawBytes[i] = val[vi];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ClearRawBytes(byte* rawBytes, int count)
        {
            for (int i = 0; i < count; i++)
            {
                rawBytes[i] = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool AreRawBytesCleared(byte* rawBytes, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (rawBytes[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SwapBitShift(long value)
        {
            ulong uvalue = (ulong)value;
            ulong swapped = (0x00000000000000FF) & (uvalue >> 56) |
                            (0x000000000000FF00) & (uvalue >> 40) |
                            (0x0000000000FF0000) & (uvalue >> 24) |
                            (0x00000000FF000000) & (uvalue >> 8) |
                            (0x000000FF00000000) & (uvalue << 8) |
                            (0x0000FF0000000000) & (uvalue << 24) |
                            (0x00FF000000000000) & (uvalue << 40) |
                            (0xFF00000000000000) & (uvalue << 56);
            return (long)swapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SwapBitShift(int value)
        {
            uint uvalue = (uint)value;
            uint swapped = (0x000000FF) & (uvalue << 24) |
                           (0x0000FF00) & (uvalue << 8) |
                           (0x00FF0000) & (uvalue >> 8) |
                           (0xFF000000) & (uvalue >> 24);
            return (int)swapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SwapBitShift(short value)
        {
            uint uvalue = (uint)value;
            uint swapped = (0x000000FF) & (uvalue << 8) |
                           (0x0000FF00) & (uvalue >> 8);
            return (short)swapped;
        }
    }
}
