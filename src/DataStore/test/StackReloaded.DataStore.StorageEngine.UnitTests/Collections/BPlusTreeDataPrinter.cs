using System;
using System.Linq;
using System.Text;
using StackReloaded.DataStore.StorageEngine.Collections;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Collections
{
    public static class BPlusTreeDataPrinter
    {
        internal static string PrintData<TKey, TValue>(IInternalBPlusTree<TKey, TValue> bplusTree)
        {
            if (bplusTree == null)
            {
                throw new ArgumentNullException(nameof(bplusTree));
            }

            var sb = new StringBuilder();

            sb.AppendLine();

            PrintNode<TKey, TValue>(sb, 0, bplusTree.RootNode);

            return sb.ToString();
        }

        private static void PrintNode<TKey, TValue>(StringBuilder sb, int level, IBPlusTreeNode node)
        {
            switch (node)
            {
                case IBPlusTreeInternalNode<TKey> internalNode:
                    PrintInternalNode<TKey, TValue>(sb, level, internalNode);
                    break;
                case IBPlusTreeLeafNode<TKey, TValue> leafNode:
                    PrintLeafNode(sb, level, leafNode);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private static void PrintInternalNode<TKey, TValue>(StringBuilder sb, int level, IBPlusTreeInternalNode<TKey> internalNode)
        {
            if (internalNode == null)
            {
                throw new ArgumentNullException(nameof(internalNode));
            }

            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }

            var keys = internalNode.Keys.ToList();

            if (keys.Count == 0)
            {
                sb.Append("(empty keys)");
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(keys[i].ToString());
                }
            }

            sb.AppendLine();

            var nodePointers = internalNode.NodePointers.ToList();

            for (int i = 0; i < nodePointers.Count; i++)
            {
                PrintNode<TKey, TValue>(sb, level + 1, nodePointers[i]);
            }
        }

        private static void PrintLeafNode<TKey, TValue>(StringBuilder sb, int level, IBPlusTreeLeafNode<TKey, TValue> leafNode)
        {
            if (leafNode == null)
            {
                throw new ArgumentNullException(nameof(leafNode));
            }

            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }

            var keys = leafNode.Keys.ToList();

            if (keys.Count == 0)
            {
                sb.Append("(empty keys)");
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(keys[i].ToString());
                }
            }

            sb.AppendLine();
        }
    }
}
