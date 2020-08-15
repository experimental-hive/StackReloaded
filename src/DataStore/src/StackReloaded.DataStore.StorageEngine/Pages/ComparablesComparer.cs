using System;

namespace StackReloaded.DataStore.StorageEngine.Pages
{
    internal class ComparablesComparer
    {
        private static int Compare(ref ReadOnlySpan<IComparable> a, ref ReadOnlySpan<IComparable> b)
        {
            if (a.Length != b.Length)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0, c = a.Length; i < c; i++)
            {
                IComparable ai = a[i];
                IComparable bi = b[i];

                if (ai == null && bi == null)
                {
                    continue; // two null items are assumed 'equal'
                }
                else if (ai == null)
                {
                    // arbitrary: non-null item is considered 'greater than' null item
                    return -1; // "a < b"
                }
                else if (bi == null)
                {
                    return 1; // "a > b"
                }

                int comp = ai.CompareTo(bi);
                if (comp != 0)
                {
                    return comp;
                }
            }

            return 0;
        }
    }
}
