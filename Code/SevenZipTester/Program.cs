using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace SevenZipTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SevenZipHelper.SevenZipHelper hola = new SevenZipHelper.SevenZipHelper(1048576);
            byte[] data = new byte[1048576];
            byte[] data2 = new byte[1048576];
            FileStream craft = new FileStream("Wales Class Battlecruiser Mk II.craft", FileMode.Open);
            FileStream output = new FileStream("ship.craft", FileMode.Create);
            FileStream tmp = new FileStream("OUT.7z", FileMode.Create);
            MemoryStream outputM = new MemoryStream();
            //MemoryStream outputM = new MemoryStream(data);
            hola.Compress(craft, data);
            //hola.Compress(craft, outputM);
            //byte[] bytes = outputM.ToArray();
            //tmp.Write(bytes, 0,bytes.Length);
            tmp.Write(data, 0, (int)hola.OutputBytesCount + 13);
            Console.WriteLine(hola.OutputBytesCount);
            hola.Decompress(tmp, output);
            //hola.Decompress(data, (int)hola.OutputBytesCount+13, data2);
            //output.Write(data2, 0, (int)hola.OutputBytesCount);
            output.Close();
            tmp.Close();
            //outputM.Close();
            Console.ReadLine();
        }
    }
}
