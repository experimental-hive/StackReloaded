﻿using System;
using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using StackReloaded.DataStore.StorageEngine.Collections;
using Xunit;

namespace StackReloaded.DataStore.StorageEngine.UnitTests.Collections
{
    public class BPlusTreeTests
    {
        [Fact]
        public void GivenOrderOf3WhenCreateBPlusTreeThenArgumentException()
        {
            // arrange
            var order = 2;
            var keyComparer = A.Fake<IComparer<int>>();

            // act
            var act = (Func<BPlusTree<int, int>>)(() => new BPlusTree<int, int>(order, keyComparer));

            // assert
            act.Should().Throw<ArgumentException>().And.Message.Should().Be("The tree must have order of at least 3. (Parameter 'order')");
        }

        [Fact]
        public void GivenWhenOrderOf3WhenCreateBPlusTreeThenBPlusTreeOfOrder3Created()
        {
            // arrange
            var order = 3;
            var keyComparer = A.Fake<IComparer<int>>();

            // act
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);

            // assert
            bplusTree.Order.Should().Be(3);
            bplusTree.RootNode.Should().NotBeNull();
            bplusTree.RootNode.IsLeaf.Should().BeTrue();
            var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
            rootNode.Keys.Should().BeEmpty();
            rootNode.Values.Should().BeEmpty();
        }

        [Fact]
        public void GivenEmptyBPlusTreeOfOrder4WhenInsert10EntriesNotSortedByKeyOneByOneThenBPlusTreeOfOrder4HasCorrectStateWith10InsertedEntries()
        {
            // Example from https://www.cs.nmsu.edu/~hcao/teaching/cs582/note/DB2_4_BplusTreeExample.pdf
            // Insert 1, 3, 5, 7, 9, 2, 4, 6, 8, 10

            // arrange
            var order = 4;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);
            
            // act 1: Insert 1
            bplusTree.Insert(1, 100);

            // assert 1
            {
                bplusTree.RootNode.IsLeaf.Should().BeTrue();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                rootNode.Keys.Should().HaveCount(1);
                rootNode.Values.Should().HaveCount(1);
                rootNode.Keys[0].Should().Be(1);
                rootNode.Values[0].Should().Be(100);
            }

            // act 2: Insert 3, 5
            bplusTree.Insert(3, 300);
            bplusTree.Insert(5, 500);

            // assert 2
            {
                bplusTree.RootNode.IsLeaf.Should().BeTrue();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                rootNode.Keys.Should().HaveCount(3);
                rootNode.Values.Should().HaveCount(3);
                rootNode.Keys[0].Should().Be(1);
                rootNode.Values[0].Should().Be(100);
                rootNode.Keys[1].Should().Be(3);
                rootNode.Values[1].Should().Be(300);
                rootNode.Keys[2].Should().Be(5);
                rootNode.Values[2].Should().Be(500);
            }

            // act 3: Insert 7
            bplusTree.Insert(7, 700);

            // assert 3
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(1);
                rootNode.NodePointers.Should().HaveCount(2);
                rootNode.Keys[0].Should().Be(5);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(3);
                leafNode0.Values[1].Should().Be(300);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(2);
                leafNode1.Keys[0].Should().Be(5);
                leafNode1.Values[0].Should().Be(500);
                leafNode1.Keys[1].Should().Be(7);
                leafNode1.Values[1].Should().Be(700);
            }

            // act 4: Insert 9
            bplusTree.Insert(9, 900);

            // assert 4
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(1);
                rootNode.NodePointers.Should().HaveCount(2);
                rootNode.Keys[0].Should().Be(5);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(3);
                leafNode0.Values[1].Should().Be(300);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(3);
                leafNode1.Keys[0].Should().Be(5);
                leafNode1.Values[0].Should().Be(500);
                leafNode1.Keys[1].Should().Be(7);
                leafNode1.Values[1].Should().Be(700);
                leafNode1.Keys[2].Should().Be(9);
                leafNode1.Values[2].Should().Be(900);
            }

            // act 5: Insert 2
            bplusTree.Insert(2, 200);

            // assert 5
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(1);
                rootNode.NodePointers.Should().HaveCount(2);
                rootNode.Keys[0].Should().Be(5);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(3);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(2);
                leafNode0.Values[1].Should().Be(200);
                leafNode0.Keys[2].Should().Be(3);
                leafNode0.Values[2].Should().Be(300);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(3);
                leafNode1.Keys[0].Should().Be(5);
                leafNode1.Values[0].Should().Be(500);
                leafNode1.Keys[1].Should().Be(7);
                leafNode1.Values[1].Should().Be(700);
                leafNode1.Keys[2].Should().Be(9);
                leafNode1.Values[2].Should().Be(900);
            }

            // act 6: Insert 4
            bplusTree.Insert(4, 400);

            // assert 6
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(2);
                rootNode.NodePointers.Should().HaveCount(3);
                rootNode.Keys[0].Should().Be(3);
                rootNode.Keys[1].Should().Be(5);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(2);
                leafNode0.Values[1].Should().Be(200);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(2);
                leafNode1.Keys[0].Should().Be(3);
                leafNode1.Values[0].Should().Be(300);
                leafNode1.Keys[1].Should().Be(4);
                leafNode1.Values[1].Should().Be(400);
                var leafNode2 = rootNode.NodePointers[2].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode2.Keys.Should().HaveCount(3);
                leafNode2.Keys[0].Should().Be(5);
                leafNode2.Values[0].Should().Be(500);
                leafNode2.Keys[1].Should().Be(7);
                leafNode2.Values[1].Should().Be(700);
                leafNode2.Keys[2].Should().Be(9);
                leafNode2.Values[2].Should().Be(900);
            }

            // act 7: Insert 6
            bplusTree.Insert(6, 600);

            // assert 7
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(3);
                rootNode.NodePointers.Should().HaveCount(4);
                rootNode.Keys[0].Should().Be(3);
                rootNode.Keys[1].Should().Be(5);
                rootNode.Keys[2].Should().Be(7);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(2);
                leafNode0.Values[1].Should().Be(200);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(2);
                leafNode1.Keys[0].Should().Be(3);
                leafNode1.Values[0].Should().Be(300);
                leafNode1.Keys[1].Should().Be(4);
                leafNode1.Values[1].Should().Be(400);
                var leafNode2 = rootNode.NodePointers[2].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode2.Keys.Should().HaveCount(2);
                leafNode2.Keys[0].Should().Be(5);
                leafNode2.Values[0].Should().Be(500);
                leafNode2.Keys[1].Should().Be(6);
                leafNode2.Values[1].Should().Be(600);
                var leafNode3 = rootNode.NodePointers[3].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode3.Keys.Should().HaveCount(2);
                leafNode3.Keys[0].Should().Be(7);
                leafNode3.Values[0].Should().Be(700);
                leafNode3.Keys[1].Should().Be(9);
                leafNode3.Values[1].Should().Be(900);
            }

            // act 8: Insert 8
            bplusTree.Insert(8, 800);

            // assert 8
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(3);
                rootNode.NodePointers.Should().HaveCount(4);
                rootNode.Keys[0].Should().Be(3);
                rootNode.Keys[1].Should().Be(5);
                rootNode.Keys[2].Should().Be(7);
                var leafNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(2);
                leafNode0.Values[1].Should().Be(200);
                var leafNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(2);
                leafNode1.Keys[0].Should().Be(3);
                leafNode1.Values[0].Should().Be(300);
                leafNode1.Keys[1].Should().Be(4);
                leafNode1.Values[1].Should().Be(400);
                var leafNode2 = rootNode.NodePointers[2].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode2.Keys.Should().HaveCount(2);
                leafNode2.Keys[0].Should().Be(5);
                leafNode2.Values[0].Should().Be(500);
                leafNode2.Keys[1].Should().Be(6);
                leafNode2.Values[1].Should().Be(600);
                var leafNode3 = rootNode.NodePointers[3].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode3.Keys.Should().HaveCount(3);
                leafNode3.Keys[0].Should().Be(7);
                leafNode3.Values[0].Should().Be(700);
                leafNode3.Keys[1].Should().Be(8);
                leafNode3.Values[1].Should().Be(800);
                leafNode3.Keys[2].Should().Be(9);
                leafNode3.Values[2].Should().Be(900);
            }

            // act 9: Insert 10
            bplusTree.Insert(10, 1000);

            // assert 9
            {
                bplusTree.RootNode.IsLeaf.Should().BeFalse();
                var rootNode = bplusTree.RootNode.Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                rootNode.Keys.Should().HaveCount(1);
                rootNode.NodePointers.Should().HaveCount(2);
                rootNode.Keys[0].Should().Be(7);
                var internalNode0 = rootNode.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                internalNode0.Keys.Should().HaveCount(2);
                internalNode0.NodePointers.Should().HaveCount(3);
                internalNode0.Keys[0].Should().Be(3);
                internalNode0.Keys[1].Should().Be(5);
                var leafNode0 = internalNode0.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode0.Keys.Should().HaveCount(2);
                leafNode0.Keys[0].Should().Be(1);
                leafNode0.Values[0].Should().Be(100);
                leafNode0.Keys[1].Should().Be(2);
                leafNode0.Values[1].Should().Be(200);
                var leafNode1 = internalNode0.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode1.Keys.Should().HaveCount(2);
                leafNode1.Keys[0].Should().Be(3);
                leafNode1.Values[0].Should().Be(300);
                leafNode1.Keys[1].Should().Be(4);
                leafNode1.Values[1].Should().Be(400);
                var leafNode2 = internalNode0.NodePointers[2].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode2.Keys.Should().HaveCount(2);
                leafNode2.Keys[0].Should().Be(5);
                leafNode2.Values[0].Should().Be(500);
                leafNode2.Keys[1].Should().Be(6);
                leafNode2.Values[1].Should().Be(600);
                var internalNode1 = rootNode.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.InternalNode>().Subject;
                internalNode1.Keys.Should().HaveCount(1);
                internalNode1.NodePointers.Should().HaveCount(2);
                internalNode1.Keys[0].Should().Be(9);
                var leafNode3 = internalNode1.NodePointers[0].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode3.Keys.Should().HaveCount(2);
                leafNode3.Values.Should().HaveCount(2);
                leafNode3.Keys[0].Should().Be(7);
                leafNode3.Values[0].Should().Be(700);
                leafNode3.Keys[1].Should().Be(8);
                leafNode3.Values[1].Should().Be(800);
                var leafNode4 = internalNode1.NodePointers[1].Should().BeOfType<BPlusTree<int, int>.LeafNode>().Subject;
                leafNode4.Keys.Should().HaveCount(2);
                leafNode4.Values.Should().HaveCount(2);
                leafNode4.Keys[0].Should().Be(9);
                leafNode4.Values[0].Should().Be(900);
                leafNode4.Keys[1].Should().Be(10);
                leafNode4.Values[1].Should().Be(1000);
            }
        }

        [Theory]
        [InlineData(3, true, 300)]
        [InlineData(4, true, 400)]
        [InlineData(7, true, 700)]
        [InlineData(10, true, 1000)]
        [InlineData(11, false, default(int))]
        public void GivenBPlusTreeOfOrder4With10InsertedEntriesWhenSeekByKeyThenReturnsFoundFlagAndValueIfFounded(int seekKey, bool expectedFoundFlag, int expectedFoundValue)
        {
            // arrange
            var order = 4;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);
            var keys = new [] { 1, 3, 5, 7, 9, 2, 4, 6, 8, 10 };
            
            foreach(int key in keys)
            {
                bplusTree.Insert(key, key * 100);
            }

            // act
            var foundFlag = bplusTree.Seek(seekKey, out var foundValue);

            // assert
            foundFlag.Should().Be(expectedFoundFlag);
            foundValue.Should().Be(expectedFoundValue);
        }

        [Theory]
        [InlineData(0, 1, 30)]
        [InlineData(0, 2, 20)]
        [InlineData(0, 3, 25)]
        [InlineData(4, 5, 9)]
        public void GivenBPlusTreeWhenDeleteEntriesThenBPlusTreeHasCorrectState(int indexOfBPlusTreeDataBeforeDelete, int indexOfBPlusTreeDataAfterDelete, int key)
        {
            // arrange
            var bplusTreeData0BeforeDelete = @"
20
  15, 18
    9, 11, 13
    15, 17
    18, 19
  25, 30
    20, 21, 24
    25, 26
    30, 31, 33
";
            var bplusTreeData0AfterDelete30 = @"
20
  15, 18
    9, 11, 13
    15, 17
    18, 19
  25, 31
    20, 21, 24
    25, 26
    31, 33
";
            var bplusTreeData0AfterDelete20 = @"
21
  15, 18
    9, 11, 13
    15, 17
    18, 19
  25, 30
    21, 24
    25, 26
    30, 31, 33
";

            var bplusTreeData0AfterDelete25 = @"
20
  15, 18
    9, 11, 13
    15, 17
    18, 19
  26, 31
    20, 21, 24
    26, 30
    31, 33
";

            var bplusTreeData1BeforeDelete = @"
9
  3, 5, 7
    1, 2
    3, 4
    5, 6
    7, 8
  11
    9, 10
    11, 12
";

            var bplusTreeData1AfterDelete9 = @"
7
  3, 5
    1, 2
    3, 4
    5, 6
  10
    7, 8
    10, 11, 12
";
            var bplusTreeDataArray = new[]
            {
                bplusTreeData0BeforeDelete,
                bplusTreeData0AfterDelete30,
                bplusTreeData0AfterDelete20,
                bplusTreeData0AfterDelete25,
                bplusTreeData1BeforeDelete,
                bplusTreeData1AfterDelete9
            };

            var bplusTreeData = bplusTreeDataArray[indexOfBPlusTreeDataBeforeDelete];
            var expectedBPlusTreeDataAfterDelete = bplusTreeDataArray[indexOfBPlusTreeDataAfterDelete];
            var order = 3;
            var keyComparer = Comparer<int>.Default;
            var bplusTree = new BPlusTree<int, int>(order, keyComparer);
            BPlusTreeConstructor.Construct(bplusTree, bplusTreeData);
            BPlusTreeDataPrinter.PrintData(bplusTree).Should().Be(bplusTreeData);

            // act
            var deleted = bplusTree.Delete(key);

            // assert
            deleted.Should().BeTrue();
            BPlusTreeDataPrinter.PrintData(bplusTree).Should().Be(expectedBPlusTreeDataAfterDelete);
        }
    }

    public class BPlusTreeLeafNodeTests
    {
        [Fact]
        public void GivenLeafNodeThenIsLeafIsTrue()
        {
            // arrange
            var leaf = new BPlusTree<int, int>.LeafNode();

            // act
            var result = leaf.IsLeaf;

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenEmptyEntriesWhenAddEntryThenAddedEntryIsAtIndex0()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();

            // act
            leaf.AddEntry(2, keyComparer, 100);

            // assert
            leaf.Keys.Should().HaveCount(1);
            leaf.Values.Should().HaveCount(1);
            leaf.Keys[0].Should().Be(2);
            leaf.Values[0].Should().Be(100);
        }

        [Fact]
        public void GivenEntryWithKey2IsAtIndex0WhenAddEntryWithKey1ThenAddedEntryWithKey1IsAtIndex0()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();
            leaf.AddEntry(2, keyComparer, 100);

            // act
            leaf.AddEntry(1, keyComparer, 200);

            // assert
            leaf.Keys.Should().HaveCount(2);
            leaf.Values.Should().HaveCount(2);
            leaf.Keys[0].Should().Be(1);
            leaf.Values[0].Should().Be(200);
            leaf.Keys[1].Should().Be(2);
            leaf.Values[1].Should().Be(100);
        }

        [Fact]
        public void GivenEntryWithKey2IsAtIndex0WhenAddEntryWithKey3ThenAddedEntryWithKey3IsAtIndex1()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();
            leaf.AddEntry(2, keyComparer, 100);

            // act
            leaf.AddEntry(3, keyComparer, 200);

            // assert
            leaf.Keys.Should().HaveCount(2);
            leaf.Values.Should().HaveCount(2);
            leaf.Keys[0].Should().Be(2);
            leaf.Values[0].Should().Be(100);
            leaf.Keys[1].Should().Be(3);
            leaf.Values[1].Should().Be(200);
        }

        [Fact]
        public void Given9EntriesWhenSplitThen4EntriesMovedToNextNewLeaf()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();
            var oldNextLeaf = new BPlusTree<int, int>.LeafNode();
            leaf.NextLeaf = oldNextLeaf;

            for (int i = 1; i <= 9; i++)
            {
                leaf.AddEntry(i, keyComparer, i * 100);
            }

            // act
            var newNextLeaf = leaf.Split();

            // assert
            leaf.Keys.Should().HaveCount(5);
            leaf.Values.Should().HaveCount(5);
            for (int i = 0; i < 5; i++)
            {
                leaf.Keys[i].Should().Be(i + 1);
                leaf.Values[i].Should().Be((i + 1) * 100);
            }
            newNextLeaf.Keys.Should().HaveCount(4);
            newNextLeaf.Values.Should().HaveCount(4);
            for (int i = 0; i < 4; i++)
            {
                newNextLeaf.Keys[i].Should().Be(i + 1 + 5);
                newNextLeaf.Values[i].Should().Be((i + 1 + 5) * 100);
            }
            leaf.NextLeaf.Should().BeSameAs(newNextLeaf);
            newNextLeaf.PreviousLeaf.Should().BeSameAs(leaf);
            newNextLeaf.NextLeaf.Should().BeSameAs(oldNextLeaf);
        }

        [Fact]
        public void GivenEmptyEntriesWhenDeleteEntryThenNoEntryIsDeleted()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();

            // act
            var deleted = leaf.DeleteEntry(2, keyComparer, out var index);

            // assert
            deleted.Should().BeFalse();
            index.Should().Be(-1);
            leaf.Keys.Should().HaveCount(0);
            leaf.Values.Should().HaveCount(0);
        }

        [Fact]
        public void GivenThreeEntriesWithKeys1And2And3WhenDeleteEntryWithKey2ThenEntryWithKey3IsAtIndex1()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();
            leaf.AddEntry(1, keyComparer, 100);
            leaf.AddEntry(2, keyComparer, 200);
            leaf.AddEntry(3, keyComparer, 300);

            // act
            var deleted = leaf.DeleteEntry(2, keyComparer, out var index);

            // assert
            deleted.Should().BeTrue();
            index.Should().Be(1);
            leaf.Keys.Should().HaveCount(2);
            leaf.Values.Should().HaveCount(2);
            leaf.Keys[0].Should().Be(1);
            leaf.Values[0].Should().Be(100);
            leaf.Keys[1].Should().Be(3);
            leaf.Values[1].Should().Be(300);
        }

        [Fact]
        public void GivenThreeEntriesWithKeys1And2And3WhenDeleteEntryWithKey4ThenNoEntryIsDeleted()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var leaf = new BPlusTree<int, int>.LeafNode();
            leaf.AddEntry(1, keyComparer, 100);
            leaf.AddEntry(2, keyComparer, 200);
            leaf.AddEntry(3, keyComparer, 300);

            // act
            var deleted = leaf.DeleteEntry(4, keyComparer, out var index);

            // assert
            deleted.Should().BeFalse();
            index.Should().Be(-1);
            leaf.Keys.Should().HaveCount(3);
            leaf.Values.Should().HaveCount(3);
            leaf.Keys[0].Should().Be(1);
            leaf.Values[0].Should().Be(100);
            leaf.Keys[1].Should().Be(2);
            leaf.Values[1].Should().Be(200);
            leaf.Keys[2].Should().Be(3);
            leaf.Values[2].Should().Be(300);
        }
    }

    public class BPlusTreeInternalNodeTests
    {
        [Fact]
        public void GivenInternalNodeThenIsLeafIsFalse()
        {
            // arrange
            var internalNode = new BPlusTree<int, int>.InternalNode();

            // act
            var result = internalNode.IsLeaf;

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenEmptyEntriesWhenAddEntryThenAddedEntryIsAtIndex0()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            var key2NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key2NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();

            // act
            internalNode.AddEntry(2, keyComparer, key2NodePointerLeft, key2NodePointerRight);

            // assert
            internalNode.Keys.Should().HaveCount(1);
            internalNode.NodePointers.Should().HaveCount(2);
            internalNode.Keys[0].Should().Be(2);
            internalNode.NodePointers[0].Should().BeSameAs(key2NodePointerLeft);
            internalNode.NodePointers[1].Should().BeSameAs(key2NodePointerRight);
        }

        [Fact]
        public void GivenEntryWithKey2IsAtIndex0WhenAddEntryWithKey1ThenAddedEntryWithKey1IsAtIndex0()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            var key2NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key2NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();
            internalNode.AddEntry(2, keyComparer, key2NodePointerLeft, key2NodePointerRight);
            var key1NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key1NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();

            // act
            internalNode.AddEntry(1, keyComparer, key1NodePointerLeft, key1NodePointerRight);

            // assert
            internalNode.Keys.Should().HaveCount(2);
            internalNode.NodePointers.Should().HaveCount(3);
            internalNode.Keys[0].Should().Be(1);
            internalNode.Keys[1].Should().Be(2);
            internalNode.NodePointers[0].Should().BeSameAs(key1NodePointerLeft);
            internalNode.NodePointers[1].Should().BeSameAs(key1NodePointerRight);
            internalNode.NodePointers[2].Should().BeSameAs(key2NodePointerRight);
        }

        [Fact]
        public void GivenEntryWithKey2IsAtIndex0WhenAddEntryWithKey3ThenAddedEntryWithKey3IsAtIndex1()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            var key2NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key2NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();
            internalNode.AddEntry(2, keyComparer, key2NodePointerLeft, key2NodePointerRight);
            var key3NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key3NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();

            // act
            internalNode.AddEntry(3, keyComparer, key3NodePointerLeft, key3NodePointerRight);

            // assert
            internalNode.Keys.Should().HaveCount(2);
            internalNode.NodePointers.Should().HaveCount(3);
            internalNode.Keys[0].Should().Be(2);
            internalNode.Keys[1].Should().Be(3);
            internalNode.NodePointers[0].Should().BeSameAs(key2NodePointerLeft);
            internalNode.NodePointers[1].Should().BeSameAs(key3NodePointerLeft);
            internalNode.NodePointers[2].Should().BeSameAs(key3NodePointerRight);
        }

        [Fact]
        public void GivenEntryWithKey2WhenAddEntryWithSameKey2ThenAddedEntryWithKey2IsAtSameIndexButWithUpdatedLeftAndRightNodePointer()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            var key2NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var key2NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();
            internalNode.AddEntry(2, keyComparer, key2NodePointerLeft, key2NodePointerRight);
            var updatedKey2NodePointerLeft = A.Fake<BPlusTree<int, int>.INode>();
            var updatedKey2NodePointerRight = A.Fake<BPlusTree<int, int>.INode>();

            // act
            internalNode.AddEntry(2, keyComparer, updatedKey2NodePointerLeft, updatedKey2NodePointerRight);

            // assert
            internalNode.Keys.Should().HaveCount(1);
            internalNode.NodePointers.Should().HaveCount(2);
            internalNode.Keys[0].Should().Be(2);
            internalNode.NodePointers[0].Should().BeSameAs(updatedKey2NodePointerLeft);
            internalNode.NodePointers[1].Should().BeSameAs(updatedKey2NodePointerRight);
        }

        [Fact]
        public void Given9EntriesWhenSplitThen4EntriesMovedToNextNewInternalNode()
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            var nodePointers = new BPlusTree<int, int>.INode[9 + 1];

            for (int i = 0; i <= 9; i++)
            {
                nodePointers[i] = A.Fake<BPlusTree<int, int>.INode>();
            }

            for (int i = 1; i <= 9; i++)
            {
                internalNode.AddEntry(i, keyComparer, nodePointers[i - 1], nodePointers[i]);
            }

            // act
            var newRightInternalNode = internalNode.Split();

            // assert
            internalNode.Keys.Should().HaveCount(5);
            internalNode.NodePointers.Should().HaveCount(6);
            for (int i = 0; i < 5; i++)
            {
                internalNode.Keys[i].Should().Be(i + 1);
                internalNode.NodePointers[i].Should().BeSameAs(nodePointers[i]);
            }
            internalNode.NodePointers[5].Should().BeSameAs(nodePointers[5]);
            newRightInternalNode.Keys.Should().HaveCount(4);
            newRightInternalNode.NodePointers.Should().HaveCount(5);
            for (int i = 0; i < 4; i++)
            {
                newRightInternalNode.Keys[i].Should().Be(i + 1 + 5);
                newRightInternalNode.NodePointers[i].Should().BeSameAs(nodePointers[i + 5]);
            }
            newRightInternalNode.NodePointers[4].Should().BeSameAs(nodePointers[4 + 5]);
        }

        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(1, true, 1)]
        [InlineData(2, false, 1)]
        [InlineData(3, true, 2)]
        [InlineData(4, false, 2)]
        public void GivenEntryWithKey1IsAtIndex0AndKey3IsAtIndex1WhenGetIndexOfNodePointerWithKeyMatchFlagThenKeyMatchFlagAndIndexShouldBeAsExpected(int key, bool expectedKeyMatchFlag, int expectedIndex)
        {
            // arrange
            var keyComparer = Comparer<int>.Default;
            var internalNode = new BPlusTree<int, int>.InternalNode();
            internalNode.Keys.Add(1);
            internalNode.Keys.Add(3);

            // act
            var keyMatchFlag = internalNode.GetIndexOfNodePointerWithKeyMatchFlag(key, keyComparer, out var index);

            // assert
            keyMatchFlag.Should().Be(expectedKeyMatchFlag);
            index.Should().Be(expectedIndex);
        }
    }
}
