using System.Collections.Generic;
using FakeItEasy;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Collections
{
    public class SimpleInMemoryBPlusTreeTests : AbstractBPlusTreeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new SimpleInMemoryBPlusTree<TKey, TValue>(order, keyComparer);
        }
    }

    public class SimpleInMemoryBPlusTreeLeafNodeTests : AbstractBPlusTreeLeafNodeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new SimpleInMemoryBPlusTree<TKey, TValue>(order, keyComparer);
        }

        internal override IBPlusTreeLeafNode<TKey, TValue> CreateLeafNode<TKey, TValue>()
        {
            return new SimpleInMemoryBPlusTree<TKey, TValue>.LeafNode();
        }
    }

    public class SimpleInMemoryBPlusTreeInternalNodeTests : AbstractBPlusTreeInternalNodeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new SimpleInMemoryBPlusTree<TKey, TValue>(order, keyComparer);
        }

        internal override IBPlusTreeInternalNode<TKey> CreateInternalNode<TKey, TValue>()
        {
            return new SimpleInMemoryBPlusTree<TKey, TValue>.InternalNode();
        }

        internal override IBPlusTreeNode CreateFakeNode<TKey, TValue>()
        {
            return A.Fake<SimpleInMemoryBPlusTree<TKey, TValue>.INode>();
        }
    }
}
