using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace PolarProblems
{
    class BinaryTree : PxCell
    {
        internal static readonly object[] Empty;

        private readonly Func<object, PxEntry, int> elementDepth;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptElement">polar type of element of tree</param>
        /// <param name="elementDepth">функция сравнения элемента дерева и добавляемого объекта</param>
        /// <param name="filePath"></param>
        /// <param name="readOnly"></param>
        public BinaryTree(PType ptElement, Func<object, PxEntry, int> elementDepth, string filePath,  bool readOnly = true)
            : base(PTypeTree(ptElement), filePath, readOnly)
        {
            this.elementDepth = elementDepth;
        }

        static BinaryTree()
        {
            Empty = new object[] {0, null};
        }

        private static PTypeUnion PTypeTree(PType tpElement)
        {
            var tpBtree = new PTypeUnion();
            tpBtree.Variants = new[]
            {
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("pair", new PTypeRecord(
                    new NamedType("element", tpElement),
                    new NamedType("less", tpBtree),//левое поддерево
                    new NamedType("more", tpBtree),
                    //1 - слева больше, -1 - справа больше.
                    new NamedType("balance", new PType(PTypeEnumeration.integer))))
            };
            return tpBtree;
        }
/*
        /// <summary>
        /// Поместить элемент в дерево в соответствии со значением функции сравнения,
        ///  вернуть ссылку на голову нового дерева
        /// </summary>
        /// <param name="element"></param>
        /// <returns>была ли изменена высота дерева</returns>
        public void Add(object element)
        {
            var node = Root;
            var lastUnBalanceNode = node;
            listEntries4Balance.Clear();
            int h = 0;
            while (node.Tag() != 0)
            {
                h++;
                var nodeEntry = node.UElementUnchecked(1);
                counter++;
                int cmp = elementDepth(element, nodeEntry.Field(0));
                PxEntry balanceEntry = nodeEntry.Field(3);
                var balance = (int)balanceEntry.Get();
                if (cmp == 0)
                {
                    var left = nodeEntry.Field(1).GetHead();
                    var right = nodeEntry.Field(2).GetHead();
                    node.Set(new object[]
                    {
                        1, new[]
                        {
                            element,
                            Empty,
                            Empty,
                            balance
                        }
                    });
                    node.UElementUnchecked(1).Field(1).SetHead(left);
                    node.UElementUnchecked(1).Field(2).SetHead(right);
                    return;
                }
                if (balance != 0)
                {
                    lastUnBalanceNode = node;
                    listEntries4Balance.Clear();
                }
                var goLeft = cmp < 0;
                //TODO catch overflow memory
                listEntries4Balance.Add(new KeyValuePair<PxEntry, int>(balanceEntry,
                    goLeft ? balance + 1 : balance - 1));
                node = nodeEntry.Field(goLeft ? 1 : 2);
            }
            // когда дерево пустое, организовать одиночное значение
            node.Set(new object[]
            {
                1, new[]
                {
                    element, new object[] {0, null}, new object[] {0, null}, 0
                }
            });
            if (listEntries4Balance.Count == 0) return;
            for (int i = 0; i < listEntries4Balance.Count; i++)
                listEntries4Balance[i].Key.Set(listEntries4Balance[i].Value);
            //  ChangeBalanceSlowlyLongSequence(element, lastUnBalanceNode);
            int b = listEntries4Balance[0].Value;
            if (b == 2)
                FixWithRotateRight(lastUnBalanceNode, listEntries4Balance);
            else if (b == -2)
                FixWithRotateLeft(lastUnBalanceNode, listEntries4Balance);
            //  return true;
        }*/
    }
}
