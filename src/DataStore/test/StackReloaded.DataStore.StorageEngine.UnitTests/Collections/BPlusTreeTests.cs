﻿using System.Collections.Generic;
using FakeItEasy;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Collections
{
    public class BPlusTreeTests : AbstractBPlusTreeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new BPlusTree<TKey, TValue>(order, keyComparer);
        }
    }

    public class BPlusTreeLeafNodeTests : AbstractBPlusTreeLeafNodeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new BPlusTree<TKey, TValue>(order, keyComparer);
        }

        internal override IBPlusTreeLeafNode<TKey, TValue> CreateLeafNode<TKey, TValue>()
        {
            return new BPlusTree<TKey, TValue>.LeafNode();
        }
    }

    public class BPlusTreeInternalNodeTests : AbstractBPlusTreeInternalNodeTests
    {
        internal override IInternalBPlusTree<TKey, TValue> CreateBPlusTree<TKey, TValue>(int order, IComparer<TKey> keyComparer)
        {
            return new BPlusTree<TKey, TValue>(order, keyComparer);
        }

        internal override IBPlusTreeInternalNode<TKey> CreateInternalNode<TKey, TValue>()
        {
            return new BPlusTree<TKey, TValue>.InternalNode();
        }

        internal override IBPlusTreeNode CreateFakeNode<TKey, TValue>()
        {
            return A.Fake<BPlusTree<TKey, TValue>.INode>();
        }
    }
}
