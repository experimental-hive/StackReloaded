using System;
using System.Collections.Generic;
using System.Globalization;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Collections
{
    public static class BPlusTreeConstructor
    {
        public static void Construct(IBPlusTree<int, int> bplusTree, object data)
        {
            switch (bplusTree)
            {
                case BPlusTree<int, int> castedBPlusTree:
                    Construct(castedBPlusTree, data);
                    break;
                case SimpleInMemoryBPlusTree<int, int> simpleInMemoryBPlusTree:
                    Construct(simpleInMemoryBPlusTree, data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void Construct(BPlusTree<int, int> bplusTree, object data)
        {
            if (bplusTree == null)
            {
                throw new ArgumentNullException(nameof(bplusTree));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var listOfListOfKeys = ParseBPlusTreeData(data);

            var stackOfParents = new Stack<BPlusTree<int, int>.InternalNode>();
            BPlusTree<int, int>.LeafNode previousLeafNode = null;

            for (int i = 0, c = listOfListOfKeys.Count; i < c; i++)
            {
                var listOfKeys = listOfListOfKeys[i];
                var currentLevel = listOfKeys[0];

                if (i > 0 && listOfListOfKeys[i - 1][0] > currentLevel)
                {
                    stackOfParents.Pop();
                }

                var parent = stackOfParents.Count == 0 ? null : stackOfParents.Peek();

                if (i < c - 1 && listOfListOfKeys[i + 1][0] > currentLevel)
                {
                    var internalNode = new BPlusTree<int, int>.InternalNode();

                    for (int k = 1; k < listOfKeys.Count; k++)
                    {
                        var key = listOfKeys[k];
                        internalNode.Keys.Add(key);
                    }

                    stackOfParents.Push(internalNode);

                    if (parent != null)
                    {
                        parent.NodePointers.Add(internalNode);
                    }
                    else
                    {
                        bplusTree.RootNode = internalNode;
                    }
                }
                else
                {
                    var leafNode = new BPlusTree<int, int>.LeafNode
                    {
                        PreviousLeaf = previousLeafNode
                    };

                    if (previousLeafNode != null)
                    {
                        previousLeafNode.NextLeaf = leafNode;
                    }

                    previousLeafNode = leafNode;

                    for (int k = 1; k < listOfKeys.Count; k++)
                    {
                        var key = listOfKeys[k];
                        leafNode.Keys.Add(key);
                        leafNode.Values.Add(key * 100);
                    }

                    if (parent != null)
                    {
                        parent.NodePointers.Add(leafNode);
                    }
                    else
                    {
                        bplusTree.RootNode = leafNode;
                    }
                }
            }
        }

        public static void Construct(SimpleInMemoryBPlusTree<int, int> bplusTree, object data)
        {
            if (bplusTree == null)
            {
                throw new ArgumentNullException(nameof(bplusTree));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var listOfListOfKeys = ParseBPlusTreeData(data);

            var stackOfParents = new Stack<SimpleInMemoryBPlusTree<int, int>.InternalNode>();
            SimpleInMemoryBPlusTree<int, int>.LeafNode previousLeafNode = null;

            for (int i = 0, c = listOfListOfKeys.Count; i < c; i++)
            {
                var listOfKeys = listOfListOfKeys[i];
                var currentLevel = listOfKeys[0];

                if (i > 0 && listOfListOfKeys[i - 1][0] > currentLevel)
                {
                    stackOfParents.Pop();
                }

                var parent = stackOfParents.Count == 0 ? null : stackOfParents.Peek();

                if (i < c - 1 && listOfListOfKeys[i + 1][0] > currentLevel)
                {
                    var internalNode = new SimpleInMemoryBPlusTree<int, int>.InternalNode();

                    for (int k = 1; k < listOfKeys.Count; k++)
                    {
                        var key = listOfKeys[k];
                        internalNode.Keys.Add(key);
                    }

                    stackOfParents.Push(internalNode);

                    if (parent != null)
                    {
                        parent.NodePointers.Add(internalNode);
                    }
                    else
                    {
                        bplusTree.RootNode = internalNode;
                    }
                }
                else
                {
                    var leafNode = new SimpleInMemoryBPlusTree<int, int>.LeafNode
                    {
                        PreviousLeaf = previousLeafNode
                    };

                    if (previousLeafNode != null)
                    {
                        previousLeafNode.NextLeaf = leafNode;
                    }

                    previousLeafNode = leafNode;

                    for (int k = 1; k < listOfKeys.Count; k++)
                    {
                        var key = listOfKeys[k];
                        leafNode.Keys.Add(key);
                        leafNode.Values.Add(key * 100);
                    }

                    if (parent != null)
                    {
                        parent.NodePointers.Add(leafNode);
                    }
                    else
                    {
                        bplusTree.RootNode = leafNode;
                    }
                }
            }
        }

        private static List<List<int>> ParseBPlusTreeData(object data)
        {
            var lines = ((string)data).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var listOfListOfKeys = new List<List<int>>(lines.Length);
            foreach (var line in lines)
            {
                var level = 0;
                var doneHandlingIndentation = false;
                var parts = line.Replace(",", ", ", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal).Split(" ");
                var keys = new List<int>();
                foreach (var part in parts)
                {
                    if (!doneHandlingIndentation)
                    {
                        if (string.IsNullOrEmpty(part))
                        {
                            level++;
                            continue;
                        }
                        doneHandlingIndentation = true;
                        keys.Add(level / 2);
                    }
                    else if (string.IsNullOrEmpty(part))
                    {
                        continue;
                    }

                    var key = int.Parse(part, CultureInfo.InvariantCulture);

                    keys.Add(key);
                }
                listOfListOfKeys.Add(keys);
            }

            return listOfListOfKeys;
        }
    }
}
