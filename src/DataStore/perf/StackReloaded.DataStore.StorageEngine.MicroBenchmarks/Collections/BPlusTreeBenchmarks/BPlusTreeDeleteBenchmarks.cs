using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks.Collections.BPlusTreeBenchmarks
{
    public class BPlusTreeDeleteBenchmarks
    {
        private int[] keys;
        private SimpleInMemoryBPlusTree<int, int> simpleInMemoryBPlusTree;
        private BPlusTree<int, int> bplusTree;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.keys = new[] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
        }

        [IterationSetup]
        public void IterationSetup()
        {
            IterationSetupSimpleInMemoryBPlusTree();
            IterationSetupBPlusTree();
        }

        private void IterationSetupSimpleInMemoryBPlusTree()
        {
            var order = 4;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new SimpleInMemoryBPlusTree<int, int>(order, keyComparer);
            var keys = this.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                bplusTree.Insert(key, key * 100);
            }
            this.simpleInMemoryBPlusTree = bplusTree;
        }

        private void IterationSetupBPlusTree()
        {
            var order = 4;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);
            var keys = this.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                bplusTree.Insert(key, key * 100);
            }
            this.bplusTree = bplusTree;
        }

        [Benchmark(Baseline = true)]
        public SimpleInMemoryBPlusTree<int, int> BPlusTreeDeleteSimpleInMemory()
        {
            var bplusTree = this.simpleInMemoryBPlusTree;
            var keys = this.keys;
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                var key = keys[i];
                bplusTree.Delete(key);
            }
            return bplusTree;
        }

        [Benchmark]
        public BPlusTree<int, int> BPlusTreeDelete()
        {
            var bplusTree = this.bplusTree;
            var keys = this.keys;
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                var key = keys[i];
                bplusTree.Delete(key);
            }
            return bplusTree;
        }
    }
}
