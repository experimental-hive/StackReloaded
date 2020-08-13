using System;
using System.Collections.Generic;
using System.Linq;

namespace StackReloaded.DataStore.StorageEngine.Collections
{
    public class BPlusTree<TKey, TValue>
    {
        [ThreadStatic]
        private static readonly Stack<InternalNode> _stackParents = new Stack<InternalNode>();

        public BPlusTree(int order, IComparer<TKey> keyComparer)
        {
            if (order < 3)
            {
                throw new ArgumentException("The tree must have order of at least 3.", nameof(order));
            }

            if (keyComparer is null)
            {
                throw new ArgumentNullException(nameof(keyComparer));
            }

            Order = order;
            KeyComparer = keyComparer;
            RootNode = new LeafNode();
        }

        public int Order { get; }
        public IComparer<TKey> KeyComparer { get; }
        internal INode RootNode { get; set; }
        private int MaxKeyLength => Order - 1;
        private int MinKeyLength => (int)Math.Ceiling((decimal)Order / 2);

        public bool Seek(TKey key, out TValue value)
        {
            var keyComparer = KeyComparer;
            var node = RootNode;
            while (!node.IsLeaf)
            {
                var internalNode = (InternalNode)node;
                node = internalNode.GetNodePointer(key, keyComparer);
            }
            var leafNode = (LeafNode)node;
            return leafNode.TryGetValue(key, keyComparer, out value);
        }

        public bool Insert(TKey key, TValue value)
        {
            var keyComparer = KeyComparer;
            var stackParents = _stackParents;
            stackParents.Clear();
            var node = RootNode;
            while (!node.IsLeaf)
            {
                var parentNode = (InternalNode)node;
                stackParents.Push(parentNode);
                node = parentNode.GetNodePointer(key, keyComparer);
            }
            var leafNode = (LeafNode)node;
            var insertEntryAtIndex = leafNode.AddEntry(key, keyComparer, value);

            if (insertEntryAtIndex == -1)
            {
                return false;
            }

            var maxKeyLength = MaxKeyLength;

            // Geen overflow van leaf node.
            if (leafNode.Keys.Count <= maxKeyLength)
            {
                return false;
            }

            var leftLeafNode = leafNode;
            var rightLeafNode = leafNode.Split();

            INode leftNodePoiner = leftLeafNode;
            INode rightNodePointer = rightLeafNode;
            var firstKeyOfRightNode = rightLeafNode.Keys[0];

            while (true)
            {
                if (stackParents.Count == 0)
                {
                    var newRootNode = new InternalNode(MaxKeyLength);
                    newRootNode.Keys.Add(firstKeyOfRightNode);
                    newRootNode.NodePointers.Add(leftNodePoiner);
                    newRootNode.NodePointers.Add(rightNodePointer);
                    RootNode = newRootNode;
                    break;
                }

                var internalNode = stackParents.Pop();
                internalNode.AddEntry(firstKeyOfRightNode, keyComparer, leftNodePoiner, rightNodePointer);

                if (internalNode.Keys.Count <= maxKeyLength)
                {
                    break;
                }

                var newRightInternalNode = internalNode.Split();

                leftNodePoiner = internalNode;
                rightNodePointer = newRightInternalNode;
                firstKeyOfRightNode = newRightInternalNode.Keys[0];
                newRightInternalNode.Keys.RemoveAt(0);
                newRightInternalNode.NodePointers.RemoveAt(0);
            }

            return true;
        }

        public bool Delete(TKey key)
        {
            var keyComparer = KeyComparer;
            var stackParents = _stackParents;
            stackParents.Clear();
            var rootNode = RootNode;
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

            var minKeyLength = MinKeyLength;

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
                RootNode = leafNode;
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
                    RootNode = currentNode;
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
                Keys = new List<TKey>();
                NodePointers = new List<INode>();
            }

            public InternalNode(int capacity)
            {
                Keys = new List<TKey>(capacity);
                NodePointers = new List<INode>(capacity + 1);
            }

            public bool IsLeaf => false;

            public List<TKey> Keys { get; set; }

            public List<INode> NodePointers { get; set; }

            public bool GetIndexOfNodePointerWithKeyMatchFlag(TKey key, IComparer<TKey> keyComparer, out int index)
            {
                var keys = Keys;

                for (int i = 0, len = keys.Count; i < len; i++)
                {
                    var compareResult = keyComparer.Compare(key, keys[i]);

                    if (compareResult == 0)
                    {
                        index = i + 1;
                        return true;
                    }

                    if (compareResult == -1)
                    {
                        index = i;
                        return false;
                    }
                }

                index = keys.Count;
                return false;
            }

            public int GetIndexOfNodePointer(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = Keys;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keyComparer.Compare(key, keys[i]) == -1)
                    {
                        return i;
                    }
                }

                return keys.Count;
            }

            public INode GetNodePointer(TKey key, IComparer<TKey> keyComparer) => NodePointers[GetIndexOfNodePointer(key, keyComparer)];

            public int AddEntry(TKey key, IComparer<TKey> keyComparer, INode leftNodePointer, INode rightNodePointer)
            {
                var keys = Keys;
                var keysCount = keys.Count;
                var nodePointers = NodePointers;
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

                return insertEntryAtIndex;
            }

            public InternalNode Split()
            {
                var keys = Keys;
                var nodePointers = NodePointers;

                var entryCountToMove = keys.Count / 2;
                var entryCountToKeep = keys.Count - entryCountToMove;

                var newRightInternalNode = new InternalNode(Keys.Capacity);
                for (int i = entryCountToKeep; i < keys.Count; i++)
                {
                    newRightInternalNode.Keys.Add(keys[i]);
                    newRightInternalNode.NodePointers.Add(nodePointers[i]);
                }
                newRightInternalNode.NodePointers.Add(nodePointers[^1]);

                for (int i = entryCountToMove; i > 0; i--)
                {
                    Keys.RemoveAt(Keys.Count - 1);
                    NodePointers.RemoveAt(NodePointers.Count - 1);
                }

                return newRightInternalNode;
            }

            public TKey MergeWithRightSibling(InternalNode rightSiblingInternalNode, InternalNode parentNode, int parentKeyIndex)
            {
                var delKey = parentNode.Keys[parentKeyIndex];
                Keys.Add(delKey);
                for (var i = 0; i < rightSiblingInternalNode.Keys.Count; i++)
                {
                    Keys.Add(rightSiblingInternalNode.Keys[i]);
                    NodePointers.Add(rightSiblingInternalNode.NodePointers[i]);
                }
                NodePointers.Add(rightSiblingInternalNode.NodePointers[^1]);
                parentNode.Keys.RemoveAt(parentKeyIndex);
                parentNode.NodePointers.RemoveAt(parentKeyIndex + 1);
                return delKey;
            }
        }

        internal class LeafNode : INode
        {
            public LeafNode()
            {
                Keys = new List<TKey>();
                Values = new List<TValue>();
            }

            public LeafNode(int capacity)
            {
                Keys = new List<TKey>(capacity);
                Values = new List<TValue>(capacity + 1);
            }

            public bool IsLeaf => true;

            public List<TKey> Keys { get; set; }

            public List<TValue> Values { get; set; }

            public LeafNode PreviousLeaf { get; set; }

            public LeafNode NextLeaf { get; set; }

            public int GetIndex(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = Keys;

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keyComparer.Compare(key, keys[i]) == 0)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public TValue GetValueAtIndex(int index) => Values[index];

            public bool TryGetValue(TKey key, IComparer<TKey> keyComparer, out TValue value)
            {
                var index = GetIndex(key, keyComparer);

                if (index == -1)
                {
                    value = default;
                    return false;
                }

                value = Values[index];
                return true;
            }

            public int AddEntry(TKey key, IComparer<TKey> keyComparer, TValue value)
            {
                var keys = Keys;
                var keysCount = keys.Count;
                var values = Values;
                var insertEntryAtIndex = keysCount;

                for (int i = 0; i < keysCount; i++)
                {
                    var compareResult = keyComparer.Compare(key, keys[i]);

                    if (compareResult == 0)
                    {
                        insertEntryAtIndex = -1;
                        break;
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
                else if (insertEntryAtIndex != -1)
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

                return insertEntryAtIndex;
            }

            public bool DeleteEntry(TKey key, IComparer<TKey> keyComparer, out int index)
            {
                index = GetIndex(key, keyComparer);

                if (index == -1)
                {
                    return false;
                }

                Keys.RemoveAt(index);
                Values.RemoveAt(index);
                return true;
            }

            public LeafNode Split()
            {
                var keys = Keys;
                var values = Values;

                var entryCountToMove = keys.Count / 2;
                var entryCountToKeep = keys.Count - entryCountToMove;

                var newNextLeaf = new LeafNode(Keys.Capacity);
                for (int i = entryCountToKeep; i < keys.Count; i++)
                {
                    newNextLeaf.Keys.Add(keys[i]);
                    newNextLeaf.Values.Add(values[i]);
                }

                for (int i = entryCountToMove; i > 0; i--)
                {
                    Keys.RemoveAt(Keys.Count - 1);
                    Values.RemoveAt(Values.Count - 1);
                }

                newNextLeaf.PreviousLeaf = this;

                if (NextLeaf != null)
                {
                    newNextLeaf.NextLeaf = NextLeaf;
                    NextLeaf.PreviousLeaf = newNextLeaf;
                }

                NextLeaf = newNextLeaf;

                return newNextLeaf;
            }

            public TKey MergeWithRightSibling(LeafNode rightSiblingLeafNode, InternalNode parentNode, IComparer<TKey> keyComparer)
            {
                var firstKeyOfRightSiblingLeafNode = rightSiblingLeafNode.Keys[0];
                for (int i = 0; i < rightSiblingLeafNode.Keys.Count; i++)
                {
                    Keys.Add(rightSiblingLeafNode.Keys[i]);
                    Values.Add(rightSiblingLeafNode.Values[i]);
                }
                rightSiblingLeafNode.Keys.Clear();
                rightSiblingLeafNode.Values.Clear();
                NextLeaf = rightSiblingLeafNode.NextLeaf;
                if (NextLeaf != null)
                {
                    NextLeaf.PreviousLeaf = this;
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
