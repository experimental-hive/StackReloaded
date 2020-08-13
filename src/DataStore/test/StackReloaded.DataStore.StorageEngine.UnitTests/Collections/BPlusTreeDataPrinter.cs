using System;
using System.Text;

namespace StackReloaded.DataStore.StorageEngine.Collections
{
    public static class BPlusTreeDataPrinter
    {
        public static string PrintData<TKey, TValue>(BPlusTree<TKey, TValue> bplusTree)
        {
            if (bplusTree == null)
            {
                throw new ArgumentNullException(nameof(bplusTree));
            }

            var sb = new StringBuilder();

            sb.AppendLine();

            PrintNode(sb, 0, bplusTree.RootNode);

            return sb.ToString();
        }

        private static void PrintNode<TKey, TValue>(StringBuilder sb, int level, BPlusTree<TKey, TValue>.INode node)
        {
            switch (node)
            {
                case BPlusTree<TKey, TValue>.InternalNode internalNode:
                    PrintInternalNode(sb, level, internalNode);
                    break;
                case BPlusTree<TKey, TValue>.LeafNode leafNode:
                    PrintLeafNode(sb, level, leafNode);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private static void PrintInternalNode<TKey, TValue>(StringBuilder sb, int level, BPlusTree<TKey, TValue>.InternalNode internalNode)
        {
            if (internalNode == null)
            {
                throw new ArgumentNullException(nameof(internalNode));
            }

            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }

            if (internalNode.Keys.Count == 0)
            {
                sb.Append("(empty keys)");
            }
            else
            {
                for (int i = 0; i < internalNode.Keys.Count; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(internalNode.Keys[i].ToString());
                }
            }

            sb.AppendLine();

            for (int i = 0; i < internalNode.NodePointers.Count; i++)
            {
                PrintNode(sb, level + 1, internalNode.NodePointers[i]);
            }
        }

        private static void PrintLeafNode<TKey, TValue>(StringBuilder sb, int level, BPlusTree<TKey, TValue>.LeafNode leafNode)
        {
            if (leafNode == null)
            {
                throw new ArgumentNullException(nameof(leafNode));
            }

            for (int i = 0; i < level; i++)
            {
                sb.Append("  ");
            }

            if (leafNode.Keys.Count == 0)
            {
                sb.Append("(empty keys)");
            }
            else
            {
                for (int i = 0; i < leafNode.Keys.Count; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(leafNode.Keys[i].ToString());
                }
            }

            sb.AppendLine();
        }
    }
}
