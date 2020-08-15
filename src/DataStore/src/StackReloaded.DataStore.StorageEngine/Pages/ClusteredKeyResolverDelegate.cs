using System;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal delegate TKey ClusteredKeyResolverDelegate<TKey>(ReadOnlySpan<byte> bytes);
}
