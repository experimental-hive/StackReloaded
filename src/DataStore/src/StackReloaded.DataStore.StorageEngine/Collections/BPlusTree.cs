using System;
using System.Collections.Generic;
using System.Linq;

namespace StackReloaded.DataStore.StorageEngine.Collections
{
    public class BPlusTree<TKey, TValue>
    {
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
        private int MinKeyLength => Order / 2;

        public bool Insert(TKey key, TValue value)
        {
            var keyComparer = KeyComparer;
            var stackInternalNodes = new Stack<InternalNode>();
            var node = RootNode;
            while (!node.IsLeaf)
            {
                var internalNode = (InternalNode)node;
                stackInternalNodes.Push(internalNode);
                node = internalNode.GetNodePointer(key, keyComparer);
            }
            var leafNode = (LeafNode)node;
            var insertEntryAtIndex = leafNode.AddEntry(key, keyComparer, value);

            if (insertEntryAtIndex == -1)
            {
                return false;
            }

            var maxKeyLength = MaxKeyLength;

            if (leafNode.Keys.Count > maxKeyLength)
            {
                var leftLeafNode = leafNode;
                var rightLeafNode = leafNode.Split();
                
                INode leftNodePoiner = leftLeafNode;
                INode rightNodePointer = rightLeafNode;
                var firstKeyOfRightNode = rightLeafNode.Keys[0];

                while (true)
                {
                    if (stackInternalNodes.Count == 0)
                    {
                        var newRootNode = new InternalNode();
                        newRootNode.Keys.Add(firstKeyOfRightNode);
                        newRootNode.NodePointers.Add(leftNodePoiner);
                        newRootNode.NodePointers.Add(rightNodePointer);
                        RootNode = newRootNode;
                        break;
                    }

                    var internalNode = stackInternalNodes.Pop();
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
            }

            return true;
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

            public bool IsLeaf => false;

            public List<TKey> Keys { get; set; }

            public List<INode> NodePointers { get; set; }

            public int GetIndexOfNodePointer(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = Keys;

                for (int i = 0, len = keys.Count; i < len; i++)
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

                var newRightInternalNode = new InternalNode();
                newRightInternalNode.Keys = keys.Skip(entryCountToKeep).ToList();
                newRightInternalNode.NodePointers = nodePointers.Skip(entryCountToKeep).ToList();

                Keys = keys.Take(entryCountToKeep).ToList();
                NodePointers = nodePointers.Take(entryCountToKeep + 1).ToList();

                return newRightInternalNode;
            }
        }

        internal class LeafNode : INode
        {
            public LeafNode()
            {
                Keys = new List<TKey>();
                Values = new List<TValue>();
            }

            public bool IsLeaf => true;

            public List<TKey> Keys { get; set; }

            public List<TValue> Values { get; set; }

            public LeafNode PreviousLeaf { get; set; }

            public LeafNode NextLeaf { get; set; }

            public int GetIndex(TKey key, IComparer<TKey> keyComparer)
            {
                var keys = Keys;

                for (int i = 0, len = keys.Count; i < len; i++)
                {
                    if (keyComparer.Compare(key, keys[i]) == 0)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public TValue GetValueAtIndex(int index) => Values[index];

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

            public LeafNode Split()
            {
                var keys = Keys;
                var values = Values;

                var entryCountToMove = keys.Count / 2;
                var entryCountToKeep = keys.Count - entryCountToMove;

                var newNextLeaf = new LeafNode();
                newNextLeaf.Keys = keys.Skip(entryCountToKeep).ToList();
                newNextLeaf.Values = values.Skip(entryCountToKeep).ToList();

                Keys = keys.Take(entryCountToKeep).ToList();
                Values = values.Take(entryCountToKeep).ToList();

                newNextLeaf.PreviousLeaf = this;

                if (NextLeaf != null)
                {
                    newNextLeaf.NextLeaf = NextLeaf;
                    NextLeaf.PreviousLeaf = newNextLeaf;
                }

                NextLeaf = newNextLeaf;

                return newNextLeaf;
            }
        }
    }
}
