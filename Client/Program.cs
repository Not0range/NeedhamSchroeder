using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NeedhamSchroeder
{
    static class Program
    {
        public const int numberSize = 32;

        static void Main(string[] args)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect("127.0.0.1", 8888);

            byte[] buffer = new byte[2048];
            int count;

            count = s.Receive(buffer);
            BigInteger p = new BigInteger(buffer.Slice(0, count));
            count = s.Receive(buffer);
            BigInteger g = new BigInteger(buffer.Slice(0, count));

            Console.WriteLine("Введите секретный ключ c");
            BigInteger c;
            do
                c = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();
            while (c >= p - 1);
            BigInteger d = g.Power(c, p);

            Console.WriteLine("Введите дополнительную информацию");
            BigInteger info = BigInteger.Parse(Console.ReadLine());//new BigInteger(Encoding.UTF8.GetBytes(Console.ReadLine()));
            s.Send(d.ToByteArray());
            Thread.Sleep(100);
            s.Send(info.ToByteArray());

            count = s.Receive(buffer);
            BigInteger db = new BigInteger(buffer.Slice(0, count));
            count = s.Receive(buffer);
            BigInteger infob = new BigInteger(buffer.Slice(0, count));
            Console.WriteLine("Открытый ключ собеседника d = {0}", db);
            Console.WriteLine("Дополнительная ифнормация собеседника d = {0}", infob);

            BigInteger k1, k2, k, r, e, m;
            ElGamal enc;
            Console.WriteLine("Введите непустую строку для роли Алисы, в противном случае будет роль Боба");
            if (Console.ReadLine() != "")
            {
                Console.WriteLine("Введите первый ключ");
                k1 = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();
                Console.WriteLine("Введите произвольное число k");
                do
                    k = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();
                while (k < 1 && k >= p - 1);
                enc = new ElGamal(p, g, BigInteger.Parse(k1.ToString() + info.ToString()), db, k);
                s.Send(enc.r.ToByteArray());
                Thread.Sleep(100);
                s.Send(enc.e.ToByteArray());
                Console.WriteLine("r = {0}", enc.r);
                Console.WriteLine("e = {0}", enc.e);
                Console.WriteLine("Ожидание второго ключа...");
                

                count = s.Receive(buffer);
                r = new BigInteger(buffer.Slice(0, count));
                count = s.Receive(buffer);
                e = new BigInteger(buffer.Slice(0, count));
                m = e * r.Power(p - 1 - c, p) % p;
                if (!m.ToString().StartsWith(k1.ToString()))
                    throw new Exception();
                k2 = BigInteger.Parse(m.ToString().Remove(0, k1.ToString().Length));
                Console.WriteLine("Второй ключ = {0}", k2);

                Console.WriteLine("Введите произвольное число k");
                do
                    k = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();
                while (k < 1 && k >= p - 1);
                enc = new ElGamal(p, g, k2, db, k);
                s.Send(enc.r.ToByteArray());
                Thread.Sleep(100);
                s.Send(enc.e.ToByteArray());
            }
            else
            {
                Console.WriteLine("Ожидание первого ключа...");
                count = s.Receive(buffer);
                r = new BigInteger(buffer.Slice(0, count));
                count = s.Receive(buffer);
                e = new BigInteger(buffer.Slice(0, count));
                Console.WriteLine("r = {0}", r);
                Console.WriteLine("e = {0}", e);
                m = e * r.Power(p - 1 - c, p) % p;
                if (!m.ToString().EndsWith(infob.ToString()))
                    throw new Exception();
                k1 = BigInteger.Parse(m.ToString().Remove(m.ToString().Length - infob.ToString().Length));
                Console.WriteLine("Первый ключ = {0}", k1);
                Console.WriteLine("Введите второй ключ");
                k2 = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();

                Console.WriteLine("Введите произвольное число k");
                do
                    k = BigInteger.Parse(Console.ReadLine());//BigRandom.RandomBig();
                while (k < 1 && k >= p - 1);
                enc = new ElGamal(p, g, BigInteger.Parse(k1.ToString() + k2.ToString()), db, k);
                s.Send(enc.r.ToByteArray());
                Thread.Sleep(100);
                s.Send(enc.e.ToByteArray());
                Console.WriteLine("r = {0}", enc.r);
                Console.WriteLine("e = {0}", enc.e);
                Console.WriteLine("Ожидание подтверждения...");

                count = s.Receive(buffer);
                r = new BigInteger(buffer.Slice(0, count));
                count = s.Receive(buffer);
                e = new BigInteger(buffer.Slice(0, count));
                Console.WriteLine("r = {0}", r);
                Console.WriteLine("e = {0}", e);
                m = e * r.Power(p - 1 - c, p) % p;
                if (m != k2)
                    throw new Exception();
            }
            s.Shutdown(SocketShutdown.Both);
            s.Disconnect(false);
            s.Close();
            Console.WriteLine("Общий секретный ключ: {0}", k1 ^ k2);
            Console.ReadLine();
        }

        static byte[] Slice(this byte[] arr, int index, int count)
        {
            byte[] temp = new byte[count];
            Array.Copy(arr, index, temp, 0, count);
            return temp;
        }

        static byte[] Concat(this byte[] arr1, byte[] arr2)
        {
            byte[] temp = new byte[arr1.Length + arr2.Length];
            Array.Copy(arr1, 0, temp, 0, arr1.Length);
            Array.Copy(arr2, 0, temp, arr1.Length, arr2.Length);
            return temp;
        }

        static bool StartsWith(this byte[] arr1, byte[] arr2)
        {
            for (int i = 0; i < arr2.Length; i++)
                if (arr1[i] != arr2[i])
                    return false;
            return true;
        }

        static bool EndsWith(this byte[] arr1, byte[] arr2)
        {
            for (int i = 0; i < arr2.Length; i++)
                if (arr1[arr1.Length - 1 - i] != arr2[arr2.Length - 1 - i])
                    return false;
            return true;
        }
    }

    static class BigRandom
    {
        public static BigInteger RandomBig()
        {
            Random rand = new Random();
            byte[] bytes = new byte[Program.numberSize / 8];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)rand.Next(256);
            bytes[bytes.Length - 1] &= 0x7F;
            return new BigInteger(bytes);
        }

        public static BigInteger CommonRandomBig()
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

    class ElGamal
    {
        public BigInteger m;

        public BigInteger p;

        public BigInteger g;

        public BigInteger d;

        public BigInteger k;

        public BigInteger r;

        public BigInteger e;

        public ElGamal(BigInteger p, BigInteger g, BigInteger m, BigInteger d, BigInteger k)
        {
            this.p = p;
            this.g = g;
            this.m = m;
            this.d = d;
            this.k = k;
            Calculate();
        }

        private void Calculate()
        {
            r = Math.Power(g, k, p);
            e = m * Math.Power(d, k, p) % p;
        }
    }

    class GCD
    {
        public int a;

        public int b;

        public int gcd;

        public int x;

        public int y;

        public GCD(int a, int b)
        {
            if (a > b)
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
            Calculate();
        }

        private void Calculate()
        {
            int[] u = new int[] { a, 1, 0 };
            int[] v = new int[] { b, 0, 1 };
            int[] t = new int[3];
            int q;

            while (v[0] != 0)
            {
                q = u[0] / v[0];
                t[0] = u[0] % v[0];
                t[1] = u[1] - q * v[1];
                t[2] = u[2] - q * v[2];
                Array.Copy(v, u, 3);
                Array.Copy(t, v, 3);
            }
            gcd = u[0];
            x = u[1];
            y = u[2];
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
            return this.GetEnumerator();
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
