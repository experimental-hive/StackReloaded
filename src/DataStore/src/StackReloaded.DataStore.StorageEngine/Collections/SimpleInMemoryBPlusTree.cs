using System;
using System.Collections.Generic;

namespace StackReloaded.DataStore.StorageEngine.Collections
{
    public class SimpleInMemoryBPlusTree<TKey, TValue> 
    {
        [ThreadStatic]
        private static readonly Stack<InternalNode> RentedStackParents = new Stack<InternalNode>();

        public SimpleInMemoryBPlusTree(int order, IComparer<TKey> keyComparer)
        {
            if (order < 3)
            {
                throw new ArgumentException("The tree must have order of at least 3.", nameof(order));
            }

            if (keyComparer is null)
            {
                throw new ArgumentNullException(nameof(keyComparer));
            }

            this.Order = order;
            this.KeyComparer = keyComparer;
        }

        public int Order { get; }
        public IComparer<TKey> KeyComparer { get; }
        internal INode RootNode { get; set; }
        private int MaxKeyLength => this.Order - 1;
        private int MinKeyLength => (int)Math.Ceiling((decimal)this.Order / 2);

        public bool Seek(TKey key, out TValue value)
        {
            var keyComparer = this.KeyComparer;
            var node = this.RootNode;
            while (!node.IsLeaf)
            {
                var internalNode = (InternalNode)node;
                node = internalNode.GetNodePointer(key, this);
            }
            var leafNode = (LeafNode)node;
            return leafNode.TryGetValue(key, keyComparer, out value);
        }

        public void Insert(TKey key, TValue value)
        {
            var node = this.RootNode;

            if (node == null)
            {
                this.RootNode = node = new LeafNode(this.MaxKeyLength);
            }

            var stackParents = RentedStackParents;
            stackParents.Clear();

            // Perform a search to determine what leaf node the new record should go into.
            while (!node.IsLeaf)
            {
                var parentNode = (InternalNode)node;
                stackParents.Push(parentNode);
                node = parentNode.GetNodePointer(key, this);
            }

            var leafNode = (LeafNode)node;

            // Add the record.
            // If the leaf node is overflowed, then it is splitted and the new right leaf node is returned.
            var rightLeafNode = leafNode.AddEntry(key, value, this);

            if (rightLeafNode == null)
            {
                return;
            }

            var leftLeafNode = leafNode;

            INode leftNodePoiner = leftLeafNode;
            INode rightNodePointer = rightLeafNode;
            var firstKeyOfRightNode = rightLeafNode.Keys[0];

            // Insert the smallest key and pointer of the right node into the parent.
            do
            {
                // If the root splits, create a new root which has one key and two pointers.
                if (stackParents.Count == 0)
                {
                    var newRootNode = new InternalNode(this.MaxKeyLength);
                    newRootNode.Keys.Add(firstKeyOfRightNode);
                    newRootNode.NodePointers.Add(leftNodePoiner);
                    newRootNode.NodePointers.Add(rightNodePointer);
                    this.RootNode = newRootNode;
                    break;
                }

                var internalNode = stackParents.Pop();

                var (newRightInternalNode, removedFirstKeyOfRightNode) = internalNode.AddEntry(firstKeyOfRightNode, leftNodePoiner, rightNodePointer, this);

                if (newRightInternalNode == null)
                {
                    break;
                }

                leftNodePoiner = internalNode;
                rightNodePointer = newRightInternalNode;
                firstKeyOfRightNode = removedFirstKeyOfRightNode;
            }
            while (true);
        }

        public bool Delete(TKey key)
        {
            var rootNode = this.RootNode;
            if (rootNode == null)
            {
                return false;
            }

            var keyComparer = this.KeyComparer;
            var stackParents = RentedStackParents;
            stackParents.Clear();
            InternalNode parentNode = null;
            var node = rootNode;
            var nodePointerIndex = -1;
            while (!node.IsLeaf)
            {
                parentNode = (InternalNode)node;
                nodePointerIndex = parentNode.GetIndexOfNodePointer(key, keyComparer);
                node = parentNode.NodePointers[nodePointerIndex];

                stackParents.Push(parentNode);
            }
            var leafNode = (LeafNode)node;
            var deleted = leafNode.DeleteEntry(key, keyComparer, out var indexOfDeletedEntry);

            if (leafNode == rootNode)
            {
                return deleted;
            }

            if (!deleted)
            {
                return false;
            }

            // De key is aanwezig in internal nodes, dus de key vervangen met minimum key uit leaf node.
            if (indexOfDeletedEntry == 0)
            {
                var toKey = leafNode.Keys[0];
                ReplaceKey(stackParents, key, toKey, keyComparer);
            }

            var minKeyLength = this.MinKeyLength;

            // Geen underflow van leaf node.
            if (leafNode.Keys.Count >= minKeyLength)
            {
                return true;
            }

            var rightSiblingLeafNode = nodePointerIndex == -1 || nodePointerIndex + 1 == parentNode.NodePointers.Count ? null : (LeafNode)parentNode.NodePointers[nodePointerIndex + 1];

            // Indien mogelijk, steel eerste entry van right sibling.
            if (rightSiblingLeafNode != null && rightSiblingLeafNode.Keys.Count > minKeyLength)
            {
                var keyOfFirstEntry = rightSiblingLeafNode.Keys[0];
                var valueOfFirstEntry = rightSiblingLeafNode.Values[0];
                leafNode.Keys.Add(keyOfFirstEntry);
                leafNode.Values.Add(valueOfFirstEntry);
                rightSiblingLeafNode.Keys.RemoveAt(0);
                rightSiblingLeafNode.Values.RemoveAt(0);
                ReplaceKey(stackParents, keyOfFirstEntry, rightSiblingLeafNode.Keys[0], keyComparer);
                return true;
            }

            var leftSiblingLeafNode = nodePointerIndex == -1 || nodePointerIndex == 0 ? null : (LeafNode)parentNode.NodePointers[nodePointerIndex - 1];

            // Indien mogelijk, steel laatste entry van left sibling.
            if (leftSiblingLeafNode != null && leftSiblingLeafNode.Keys.Count > minKeyLength)
            {
                var indexOfLastEntry = leftSiblingLeafNode.Keys.Count - 1;
                var keyOfLastEntry = leftSiblingLeafNode.Keys[indexOfLastEntry];
                var valueOfLastEntry = leftSiblingLeafNode.Values[indexOfLastEntry];
                var oldFirstKeyOfLeafNode = leafNode.Keys[0];
                leafNode.Keys.Insert(0, keyOfLastEntry);
                leafNode.Values.Insert(0, valueOfLastEntry);
                leftSiblingLeafNode.Keys.RemoveAt(indexOfLastEntry);
                leftSiblingLeafNode.Values.RemoveAt(indexOfLastEntry);
                ReplaceKey(stackParents, oldFirstKeyOfLeafNode, keyOfLastEntry, keyComparer);
                return true;
            }

            TKey delKey;
            // merge met right sibling
            if (rightSiblingLeafNode != null)
            {
                delKey = leafNode.MergeWithRightSibling(rightSiblingLeafNode, parentNode, keyComparer);
            }
            else // anders merge met left sibling
            {
                delKey = leftSiblingLeafNode.MergeWithRightSibling(leafNode, parentNode, keyComparer);
            }

            if (stackParents.Count == 1 && parentNode.Keys.Count == 0)
            {
                this.RootNode = leafNode;
                return true;
            }

            var currentNode = stackParents.Pop();

            while (currentNode.Keys.Count < minKeyLength && stackParents.Count > 0)
            {
                parentNode = stackParents.Pop();
                nodePointerIndex = parentNode.GetIndexOfNodePointer(delKey, keyComparer);

                var rightSiblingInternalNode = nodePointerIndex == -1 || nodePointerIndex + 1 == parentNode.NodePointers.Count ? null : (InternalNode)parentNode.NodePointers[nodePointerIndex + 1];

                // Indien mogelijk, steel eerste entry van right sibling.
                if (rightSiblingInternalNode != null && rightSiblingInternalNode.Keys.Count > minKeyLength)
                {
                    currentNode.Keys.Add(parentNode.Keys[nodePointerIndex]);
                    parentNode.Keys[nodePointerIndex] = rightSiblingInternalNode.Keys[0];
                    currentNode.NodePointers.Add(rightSiblingInternalNode.NodePointers[0]);
                    rightSiblingLeafNode.Keys.RemoveAt(0);
                    rightSiblingLeafNode.Values.RemoveAt(0);
                    break;
                }

                var leftSiblingInternalNode = nodePointerIndex == -1 || nodePointerIndex == 0 ? null : (InternalNode)parentNode.NodePointers[nodePointerIndex - 1];

                // Indien mogelijk, steel laatste entry van left sibling.
                if (leftSiblingInternalNode != null && leftSiblingInternalNode.Keys.Count > minKeyLength)
                {
                    currentNode.Keys.Insert(0, parentNode.Keys[nodePointerIndex - 1]);
                    var indexOfLastKey = leftSiblingInternalNode.Keys.Count - 1;
                    var indexOfLastValue = leftSiblingInternalNode.NodePointers.Count - 1;
                    parentNode.Keys[nodePointerIndex - 1] = leftSiblingInternalNode.Keys[indexOfLastKey];
                    currentNode.NodePointers.Insert(0, leftSiblingInternalNode.NodePointers[indexOfLastValue]);
                    leftSiblingInternalNode.Keys.RemoveAt(indexOfLastKey);
                    leftSiblingInternalNode.NodePointers.RemoveAt(indexOfLastValue);
                    break;
                }

                // Merge right sibling
                if (rightSiblingInternalNode != null)
                {
                    delKey = currentNode.MergeWithRightSibling(rightSiblingInternalNode, parentNode, nodePointerIndex);
                }
                else if (leftSiblingInternalNode != null) // anders merge met left sibling
                {
                    delKey = leftSiblingInternalNode.MergeWithRightSibling(currentNode, parentNode, nodePointerIndex - 1);
                    currentNode = leftSiblingInternalNode;
                }

                // Next level
                if (stackParents.Count == 0 && parentNode.Keys.Count == 0)
                {
                    this.RootNode = currentNode;
                    break;
                }

                currentNode = parentNode;
            }

            return true;
        }

        private static void ReplaceKey(Stack<InternalNode> stack, TKey fromKey, TKey toKey, IComparer<TKey> keyComparer)
        {
            foreach (var stackEntry in stack)
            {
                var keys = stackEntry.Keys;

                // binary search
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keyComparer.Compare(keys[i], fromKey) != 0)
                    {
                        continue;
                    }

                    keys[i] = toKey;
                    break;
                }
            }
        }

        internal interface INode
        {
            bool IsLeaf { get; }
        }

        internal class InternalNode : INode
        {
            public InternalNode()
            {
                this.Keys = new List<TKey>();
                this.NodePointers = new List<INode>();
            }

            public InternalNode(int capacity)
            {
                this.Keys = new List<TKey>(capacity);
                this.NodePointers = new List<INode>(capacity + 1);
            }

            public bool IsLeaf => false;

            public List<TKey> Keys { get; set; }

            public List<INode> NodePointers { get; set; }

            public int GetIndexOfNodePointer(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = this.Keys;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keyComparer.Compare(key, keys[i]) == -1)
                    {
                        return i;
                    }
                }

                return keys.Count;
            }

            public INode GetNodePointer(TKey key, SimpleInMemoryBPlusTree<TKey, TValue> bplusTree) => this.NodePointers[GetIndexOfNodePointer(key, bplusTree.KeyComparer)];

            public (InternalNode NewRightInternalNode, TKey RemovedFirstKeyOfRightNode) AddEntry(TKey key, INode leftNodePointer, INode rightNodePointer, SimpleInMemoryBPlusTree<TKey, TValue> bplusTree)
            {
                var keyComparer = bplusTree.KeyComparer;
                var keys = this.Keys;
                var keysCount = keys.Count;
                var nodePointers = this.NodePointers;
                var insertEntryAtIndex = keysCount;
                var sameKey = false;

                for (var i = 0; i < keysCount; i++)
                {
                    var compareResult = keyComparer.Compare(key, keys[i]);

                    if (compareResult == 0)
                    {
                        sameKey = true;
                        insertEntryAtIndex = i;
                        break;
                    }

                    if (compareResult == -1)
                    {
                        insertEntryAtIndex = i;
                        break;
                    }
                }

                if (sameKey)
                {
                    nodePointers[insertEntryAtIndex] = leftNodePointer;
                    nodePointers[insertEntryAtIndex + 1] = rightNodePointer;
                }
                else if (keys.Count == 0)
                {
                    keys.Add(key);
                    nodePointers.Add(leftNodePointer);
                    nodePointers.Add(rightNodePointer);
                }
                else if (insertEntryAtIndex == keysCount)
                {
                    keys.Add(key);
                    nodePointers[insertEntryAtIndex] = leftNodePointer;
                    nodePointers.Add(rightNodePointer);
                }
                else if (insertEntryAtIndex != -1)
                {
                    keys.Add(keys[keysCount - 1]);
                    nodePointers.Add(nodePointers[keysCount]);

                    for (var i = keysCount - 1; i > insertEntryAtIndex; i--)
                    {
                        keys[i] = keys[i - 1];
                        nodePointers[i + 1] = nodePointers[i];
                    }

                    keys[insertEntryAtIndex] = key;
                    nodePointers[insertEntryAtIndex] = leftNodePointer;
                    nodePointers[insertEntryAtIndex + 1] = rightNodePointer;
                }

                // Geen overflow van internal node.
                if (keys.Count <= bplusTree.MaxKeyLength)
                {
                    return (null, default);
                }

                var newRightInternalNode = Split();

                // The key that will gets pushed to the parent node needs to be removed from the original node.
                var removedFirstKeyOfRightNode = newRightInternalNode.Keys[0];
                newRightInternalNode.Keys.RemoveAt(0);
                newRightInternalNode.NodePointers.RemoveAt(0);

                return (newRightInternalNode, removedFirstKeyOfRightNode);
            }

            private InternalNode Split()
            {
                var keys = this.Keys;
                var nodePointers = this.NodePointers;

                var entryCountToMove = keys.Count / 2;
                var entryCountToKeep = keys.Count - entryCountToMove;

                var newRightInternalNode = new InternalNode(keys.Capacity);
                for (int i = entryCountToKeep; i < keys.Count; i++)
                {
                    newRightInternalNode.Keys.Add(keys[i]);
                    newRightInternalNode.NodePointers.Add(nodePointers[i]);
                }
                newRightInternalNode.NodePointers.Add(nodePointers[^1]);

                for (int i = entryCountToMove; i > 0; i--)
                {
                    keys.RemoveAt(keys.Count - 1);
                    nodePointers.RemoveAt(nodePointers.Count - 1);
                }

                return newRightInternalNode;
            }

            public TKey MergeWithRightSibling(InternalNode rightSiblingInternalNode, InternalNode parentNode, int parentKeyIndex)
            {
                var delKey = parentNode.Keys[parentKeyIndex];
                this.Keys.Add(delKey);
                for (var i = 0; i < rightSiblingInternalNode.Keys.Count; i++)
                {
                    this.Keys.Add(rightSiblingInternalNode.Keys[i]);
                    this.NodePointers.Add(rightSiblingInternalNode.NodePointers[i]);
                }
                this.NodePointers.Add(rightSiblingInternalNode.NodePointers[^1]);
                parentNode.Keys.RemoveAt(parentKeyIndex);
                parentNode.NodePointers.RemoveAt(parentKeyIndex + 1);
                return delKey;
            }
        }

        internal class LeafNode : INode
        {
            public LeafNode()
            {
                this.Keys = new List<TKey>();
                this.Values = new List<TValue>();
            }

            public LeafNode(int capacity)
            {
                this.Keys = new List<TKey>(capacity);
                this.Values = new List<TValue>(capacity + 1);
            }

            public bool IsLeaf => true;

            public List<TKey> Keys { get; set; }

            public List<TValue> Values { get; set; }

            public LeafNode PreviousLeaf { get; set; }

            public LeafNode NextLeaf { get; set; }

            public int GetIndex(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = this.Keys;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keyComparer.Compare(key, keys[i]) == 0)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public TValue GetValueAtIndex(int index) => this.Values[index];

            public bool TryGetValue(TKey key, IComparer<TKey> keyComparer, out TValue value)
            {
                var index = GetIndex(key, keyComparer);

                if (index == -1)
                {
                    value = default;
                    return false;
                }

                value = this.Values[index];
                return true;
            }

            public LeafNode AddEntry(TKey key, TValue value, SimpleInMemoryBPlusTree<TKey, TValue> bplusTree)
            {
                var keyComparer = bplusTree.KeyComparer;
                var keys = this.Keys;
                var keysCount = keys.Count;
                var values = this.Values;
                var insertEntryAtIndex = keysCount;

                for (int i = 0; i < keysCount; i++)
                {
                    var compareResult = keyComparer.Compare(key, keys[i]);

                    if (compareResult == 0)
                    {
                        throw new NotSupportedException("Duplicated key.");
                    }

                    if (compareResult == -1)
                    {
                        insertEntryAtIndex = i;
                        break;
                    }
                }

                if (insertEntryAtIndex == keys.Count)
                {
                    keys.Add(key);
                    values.Add(value);
                }
                else
                {
                    keys.Add(keys[keysCount - 1]);
                    values.Add(values[keysCount - 1]);

                    for (var i = keysCount - 1; i > insertEntryAtIndex; i--)
                    {
                        keys[i] = keys[i - 1];
                        values[i] = values[i - 1];
                    }

                    keys[insertEntryAtIndex] = key;
                    values[insertEntryAtIndex] = value;
                }

                // Geen overflow van leaf node.
                if (keys.Count <= bplusTree.MaxKeyLength)
                {
                    return null;
                }

                var rightLeafNode = Split();

                return rightLeafNode;
            }

            public bool DeleteEntry(TKey key, IComparer<TKey> keyComparer, out int index)
            {
                index = GetIndex(key, keyComparer);

                if (index == -1)
                {
                    return false;
                }

                this.Keys.RemoveAt(index);
                this.Values.RemoveAt(index);
                return true;
            }

            private LeafNode Split()
            {
                var keys = this.Keys;
                var values = this.Values;

                var entryCountToMove = keys.Count / 2;
                var entryCountToKeep = keys.Count - entryCountToMove;

                var newNextLeaf = new LeafNode(keys.Capacity);
                for (int i = entryCountToKeep; i < keys.Count; i++)
                {
                    newNextLeaf.Keys.Add(keys[i]);
                    newNextLeaf.Values.Add(values[i]);
                }

                for (int i = entryCountToMove; i > 0; i--)
                {
                    keys.RemoveAt(keys.Count - 1);
                    values.RemoveAt(values.Count - 1);
                }

                newNextLeaf.PreviousLeaf = this;

                if (this.NextLeaf != null)
                {
                    newNextLeaf.NextLeaf = this.NextLeaf;
                    this.NextLeaf.PreviousLeaf = newNextLeaf;
                }

                this.NextLeaf = newNextLeaf;

                return newNextLeaf;
            }

            public TKey MergeWithRightSibling(LeafNode rightSiblingLeafNode, InternalNode parentNode, IComparer<TKey> keyComparer)
            {
                var firstKeyOfRightSiblingLeafNode = rightSiblingLeafNode.Keys[0];
                for (int i = 0; i < rightSiblingLeafNode.Keys.Count; i++)
                {
                    this.Keys.Add(rightSiblingLeafNode.Keys[i]);
                    this.Values.Add(rightSiblingLeafNode.Values[i]);
                }
                rightSiblingLeafNode.Keys.Clear();
                rightSiblingLeafNode.Values.Clear();
                this.NextLeaf = rightSiblingLeafNode.NextLeaf;
                if (this.NextLeaf != null)
                {
                    this.NextLeaf.PreviousLeaf = this;
                }
                rightSiblingLeafNode.NextLeaf = null;
                rightSiblingLeafNode.PreviousLeaf = null;
                var keyIndex = -1;
                for (var i = 0; i < parentNode.Keys.Count; i++)
                {
                    if (keyComparer.Compare(parentNode.Keys[i], firstKeyOfRightSiblingLeafNode) == 0)
                    {
                        keyIndex = i;
                        break;
                    }
                }
                if (keyIndex != -1)
                {
                    parentNode.Keys.RemoveAt(keyIndex);
                    parentNode.NodePointers.RemoveAt(keyIndex + 1);
                }

                return firstKeyOfRightSiblingLeafNode;
            }
        }
    }
}
