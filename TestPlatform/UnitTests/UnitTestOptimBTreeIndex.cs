//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using ExtendedIndexBTree;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using PolarDB;

//namespace TestPlatform.UnitTests
//{
//    [TestClass]
//    public class UnitTestOptimBTreeIndex
//    {
//        #region Comparers
//        private Func<object, object, int> keyComparer = (object ob1, object ob2) =>
//        {
//            if ((int)ob1 < (int)ob2) return -1;
//            if ((int)ob1 > (int)ob2) return 1;
//            return 0;
//        };
//        #endregion

//        private const string path = @"../../../Databases/";

//        [TestMethod]
//        public void TestAddInTree()
//        {
//            //arrange

//            PType BTreeType = new PType(PTypeEnumeration.integer);

//            OptimBTreeInd BTreeInd = new OptimBTreeInd(BTreeType, keyComparer, keyComparer, path + "BTreeIndex.pac",degree:2);

//            object[] expected = new object[] { 0, 1, 2, 3, 4 };

//            //act
//            for (int i = 0; i < 1; ++i)
//            {
//                BTreeInd.AppendElement((object)i);
//            }

//            BTreeInd.WriteTreeInFile(@"C:\Users\Makc\Desktop\123.txt");

//            //assert
//            Assert.Fail();
//        }
//    }
//}
