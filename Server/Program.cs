using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Collections.Generic;
using System.Collections;

namespace Server
{
    static class Program
    {
        public const int numberSize = 32;

        static BigInteger p;

        static BigInteger g;

        static BigInteger[] ds = new BigInteger[2];

        static BigInteger[] infos = new BigInteger[2];

        static Socket[] clients = new Socket[2];

        static bool[] ready = new bool[2];

        static Task[] tasks = new Task[2];

        static void Main(string[] args)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
            s.Listen(2);
            Console.WriteLine("Введите p");
            p = BigInteger.Parse(Console.ReadLine());
            Console.WriteLine("Введите g");
            g = BigInteger.Parse(Console.ReadLine());
            Console.WriteLine("Сервер готов к работе");
            while(true)
            {
                Socket temp = s.Accept();
                if (clients[0] == null)
                {
                    clients[0] = temp;
                    tasks[0] = new Task(() => Listen(temp, 1));
                    tasks[0].Start();
                }
                else
                {
                    clients[1] = temp;
                    tasks[1] = new Task(() => Listen(temp, 0));
                    tasks[1].Start();
                    break;
                }
            }
            Task.WaitAll(tasks);
        }

        static void Listen(Socket s, int send)
        {
            byte[] buffer = new byte[2048];
            byte[] temp;
            int count;

            s.Send(p.ToByteArray());
            Thread.Sleep(100);
            s.Send(g.ToByteArray());

            count = s.Receive(buffer);
            ds[send ^ 1] = new BigInteger(buffer.Slice(0, count));
            Console.WriteLine("d {0}-го клиента = {1}", send ^ 1, ds[send ^ 1]);

            count = s.Receive(buffer);
            infos[send ^ 1] = new BigInteger(buffer.Slice(0, count));
            Console.WriteLine("Информация {0}-го клиента = {1}", send ^ 1, infos[send ^ 1]);
            ready[send ^ 1] = true;

            while (!ready[send]) ;
            s.Send(ds[send].ToByteArray());
            Thread.Sleep(100);
            s.Send(infos[send].ToByteArray());

            while (true)
            {
                count = s.Receive(buffer);
                if (count == 0)
                    continue;
                temp = buffer.Slice(0, count);
                Console.WriteLine(new BigInteger(temp));
                clients[send].Send(temp);
            }
        }

        static byte[] Slice(this byte[] arr, int index, int count)
        {
            byte[] temp = new byte[count];
            Array.Copy(arr, index, temp, 0, count);
            return temp;
        }
    }

    static class BigRandom
    {

        static BigInteger RandomBig()
        {
            Random rand = new Random();
            byte[] bytes = new byte[Program.numberSize / 8];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)rand.Next(256);
            bytes[bytes.Length - 1] &= 0x7F;
            return new BigInteger(bytes);
        }

        static BigInteger CommonRandomBig()
        {
            BigInteger digit = RandomBig();
            if (digit.Common())
                return digit;

            foreach (var d in new CommonDigitsEnum(digit))
                if (d > digit)
                    return d;
            return digit;
        }
    }

    static class Math
    {
        public static BigInteger Power(this BigInteger a, BigInteger x, BigInteger p)
        {
            BigInteger y = 1;
            while (x > 0)
            {
                if ((x & 1) == 1)
                    y = y * a % p;
                a = a * a % p;
                x >>= 1;
            }
            return y;
        }

        public static BigInteger Mod(this BigInteger dividend, BigInteger divisor)
        {
            while (dividend > divisor)
                dividend -= divisor;
            return dividend;
        }

        public static BigInteger PrimitiveRoot(this BigInteger digit)
        {
            BigInteger x = digit - 1;
            for (BigInteger i = 2; i < digit - 2; i++)
                if (i.Power(x, digit) == 1)
                    return i;
            return 0;
        }

        public static bool Common(this BigInteger digit)
        {
            if (digit == 2 || digit == 1)
                return true;
            if (digit.IsEven || digit.ToString().EndsWith("5"))
                return false;

            for (BigInteger i = 3; i < digit / 2; i += 2)
                if (digit % i == 0)
                    return false;
            return true;
        }
    }

    class CommonDigitsEnum : IEnumerable<BigInteger>
    {
        BigInteger digit;
        public CommonDigitsEnum()
        {
            digit = 0;
        }

        public CommonDigitsEnum(BigInteger digit)
        {
            this.digit = digit;
        }

        public IEnumerator<BigInteger> GetEnumerator()
        {
            return new CommonDigits(digit);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CommonDigits(digit);
        }
    }

    class CommonDigits : IEnumerator<BigInteger>
    {
        BigInteger current;

        public CommonDigits()
        {
            current = 0;
        }

        public CommonDigits(BigInteger digit)
        {
            current = digit;
        }

        public BigInteger Current => current;

        object IEnumerator.Current => current;

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            for (BigInteger i = (current.IsEven ? current + 1 : current + 2); ; i += 2)
            {
                if (i.Common())
                {
                    current = i;
                    return true;
                }
            }
        }

        public void Reset()
        {
            current = 0;
        }
    }
}
