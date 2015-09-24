using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolarDB;
using ExtendedIndexBTree;

namespace TestPlatform
{
    [TestClass]
    public class TestBtree
    {
        private Func<object, object, int> keyComparer = (object ob1, object ob2) =>
        {
            if ((int)ob1 < (int)ob2) return -1;
            if ((int)ob1 > (int)ob2) return 1;
            return 0;
        };

        private Func<object, object, int> elementComparer = (object ob1, object ob2) =>
        {
            object[] node1 = (object[])ob1;
            int value1 = (int)node1[1];

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private Func<object, object, int> keyComparer2 = (object ob1, object ob2) =>
        {
            int value1 = (int)ob1;

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private const string path = @"../../../Databases/";

        [TestMethod]
        public void TestDeleteKeyFromRoot()
        {
            // arrange

            //узел дерева состоит из офсета и хешкода
            PType BTreeType = new PType(PTypeEnumeration.integer);

            BTreeInd BTreeInd = new BTreeInd(BTreeType, keyComparer, keyComparer, path + "BTreeIndex.pxc");

            object[] expected = new object[] {0,1,2,3,4};

            for (int i = 0; i < 6; ++i) 
            {
                BTreeInd.AppendElement((object)i);
            }
            // act
            BTreeInd.DeleteElement((object)5);

            //BTreeInd.WriteTreeInFile(@"E:\My_Documents\Coding\_VSprojects\students\Databases\btree.txt");
            // assert
            object[] actual = (object[])BTreeInd.Root.UElement().Field(1).Get();
            for(int i=0;i<expected.Length;++i)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void TestFindFirstKey()
        {
            // arrange
            PType BTreeType = new PTypeRecord(
                new NamedType("off",new PType(PTypeEnumeration.longinteger)),
                new NamedType("key", new PType(PTypeEnumeration.integer))
                );

            BTreeInd BTreeInd = new BTreeInd(BTreeType, keyComparer2, elementComparer, path + "BTreeIndex.pxc",degree:2);

            long expected = 1700L;

            for (int i = 0; i < 50; ++i)
            {
                BTreeInd.AppendElement(new object[] { (object)((long)(i*100)), (object)i });
            }
            // act
            long actual = BTreeInd.FindFirst(17);

            // assert
            Assert.AreEqual(expected, actual);
        }
    }
}
