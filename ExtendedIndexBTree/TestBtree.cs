using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolarDB;

namespace ExtendedIndexBTree
{
    [TestClass]
    public class TestBtree
    {
        //Компаратор для сравнения узлов дерева
        private Func<object, object, int> elementComparer = (object ob1, object ob2) =>
        {
                return 1;
        };

        //Компаратор для поиска строки в дереве
        private Func<object, object, int> hashComparer = (object ob1, object ob2) =>
        {
            if ((int)ob1 > (int)ob2) return -1;
            if ((int)ob1 < (int)ob2) return 1;
            return 0;
        };

        private const string path = @"../../../Databases/";

        [TestMethod]
        public void TestDeleteKeyFromList()
        {
            // arrange

            //узел дерева состоит из офсета и хешкода
            PType BTreeType = new PType(PTypeEnumeration.integer);

            BTreeInd<string> BTreeInd = new BTreeInd<string>(100, BTreeType, hashComparer, elementComparer, path + "BTreeIndex.pxc");

            object[] expected = new object[] {0,1,2,3,4,6,7,8,9};

            for (int i = 0; i < 10; ++i)
            {
                BTreeInd.AppendElement((object)i);
            }
            // act
            BTreeInd.DeleteElement((object)5);

            // assert
            object[] actual = (object[])BTreeInd.Root.UElement().Field(1).Get();
            for(int i=0;i<expected.Length;++i)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
            //Assert.AreEqual<object []>(expected, actual, "Массивы не совпадают");
        }
    }
}
