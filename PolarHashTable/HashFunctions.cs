using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolarHashTable
{
    using HashType = UInt32;

    public class HashFunctions
    {
        ///*--- ElfHash --------------------------------------------------- *
        ///  Опубликованный алгоритм хэша использованный в формате ELF для
        ///  Объектных файлов UNIX. Принимает указатель на строку текста,
        ///  Хэширует ее и возвращает unsigned long.
        ///-----------------------------------------------------------------*/
        public HashType GetHashELF(string data)
        {
            HashType hash = 0, i;
            foreach (var symbol in data)
            {
                hash = (hash << sizeof(HashType)) + symbol;
                if ((i = hash & 0xF0000000)!= 0)
                {
                    hash ^= (i >> 24);
                    hash &= ~i;
                }
            }
            return hash;
        }

        ///--- HashPJW --------------------------------------------------- *
        ///  Адаптация обобщенного алгоритма хеширования Питера Вейнбергер (PJW) *
        ///  Основанного на версии Аллена Холуба. Принимает указатель на          *
        ///  элемент данных datum, и возвращает хэш как целое без знака    *
        ///----------------------------------------------------------------------*/
        public HashType GetHashPJW(string data)
        {
            HashType hash = 0, i;
            int multIntSizeOn6 = (int)((sizeof(HashType) * 6));
            HashType highBits = ~(HashType.MaxValue >> sizeof(HashType)); 

            foreach(var symbol in data)
            {
                hash = (hash << sizeof(HashType)) + symbol;
                if ((i = hash & highBits) != 0)
                {
                    hash = (hash ^ (i >> multIntSizeOn6)) & ~highBits;
                }
            }

            return hash;
        }

        //Аддитивный метод для строк переменной длины (очень примитивный, для примера)
        //hashValue = сумма ascii кодов символов входной строки
        public HashType GetHashAdd(string data)
        {
            HashType hash = 0;

            foreach (var symbol in data)
            {
                hash += symbol;
            }
            return hash;
        }

        //Исключающее ИЛИ для строк переменной длины (размер таблицы <= 65536)
        public HashType GetHashXor(string data, uint[] rand8)
        {
            HashType hash = 0;

            if (data == "") return hash;

            uint h1 = data[0];
            uint h2 = (uint)(data[0] + 1);

            data = data.Substring(1);

            foreach (var symbol in data)
            {
                h1 = rand8[h1 ^ symbol];
                h2 = rand8[h2 ^ symbol];
            }

            hash = ((HashType)h1 << 8) | (HashType)h2;
            return hash % (HashType)rand8.Length;
        }
    }
}
