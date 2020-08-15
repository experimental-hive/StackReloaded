using System.Collections.Generic;

namespace StackReloaded.DataStore.StorageEngine.Collections
{
    public interface IBPlusTree<TKey, TValue>
    {
        public int Order { get; }

        public bool Seek(TKey key, out TValue value);

        public void Insert(TKey key, TValue value);

        public bool Delete(TKey key);
    }

    internal interface IBPlusTreeNode
    {
        bool IsLeaf { get; }
    }

    internal interface IBPlusTreeInternalNode<TKey> : IBPlusTreeNode
    {
        public IReadOnlyList<TKey> Keys { get; }

        public IReadOnlyList<IBPlusTreeNode> NodePointers { get; }

        public (IBPlusTreeInternalNode<TKey> NewRightInternalNode, TKey RemovedFirstKeyOfRightNode) AddEntry(TKey key, IBPlusTreeNode leftNodePointer, IBPlusTreeNode rightNodePointer, IInternalBPlusTree bplusTree);
    }

    internal interface IBPlusTreeLeafNode<TKey, TValue> : IBPlusTreeNode
    {
        public IReadOnlyList<TKey> Keys { get; }

        public IReadOnlyList<TValue> Values { get; }

        public IBPlusTreeLeafNode<TKey, TValue> PreviousLeaf { get; set; }

        public IBPlusTreeLeafNode<TKey, TValue> NextLeaf { get; set; }

        public IBPlusTreeLeafNode<TKey, TValue> AddEntry(TKey key, TValue value, IInternalBPlusTree bplusTree);

        public bool DeleteEntry(TKey key, IComparer<TKey> keyComparer, out int index);
    }

    internal interface IInternalBPlusTree
    {
        internal IBPlusTreeNode RootNode { get; }
    }

    internal interface IInternalBPlusTree<TKey, TValue> : IInternalBPlusTree, IBPlusTree<TKey, TValue>
    {
    }
}
