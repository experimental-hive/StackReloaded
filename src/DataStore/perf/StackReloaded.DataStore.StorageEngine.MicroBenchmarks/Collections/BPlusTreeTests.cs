using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks.Collections
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class BPlusTreeInsertTests
    {
        private int[] _keys;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _keys = new[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
        }

        [Benchmark]
        public BPlusTree<int, int> BPlusTreeInsert()
        {
            var order = 4;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);
            foreach (int key in _keys)
            {
                bplusTree.Insert(key, key * 100);
            }
            return bplusTree;
        }
    }
}
