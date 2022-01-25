﻿using System;

namespace ModelExporter.Ifc.Types
{
    /// <summary>
    /// Conversion methods between an IFC encoded GUID string and a .NET GUID
    /// Source can be found here:
    /// https://github.com/hakonhc/IfcGuid/blob/master/IfcGuid/IfcGuid.cs
    /// </summary>
    public class IfcGuid
    {
        private static readonly char[] Base64Chars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
            'A', 'B', 'C','D', 'E', 'F', 'G', 'H', 'I', 'J', 
            'K', 'L', 'M', 'N', 'O', 'P','Q', 'R', 'S', 'T', 
            'U', 'V', 'W', 'X', 'Y', 'Z', 
            'a', 'b', 'c','d', 'e', 'f', 'g', 'h', 'i', 'j', 
            'k', 'l', 'm', 'n', 'o', 'p','q', 'r', 's', 't', 
            'u', 'v', 'w', 'x', 'y', 'z', 
            '_', '$'
        };

        /// <summary>
        /// The reverse function to calculate the number from the characters
        /// </summary>
        /// <param name="str">
        /// The char array to convert from
        /// </param>
        /// <param name="start">
        /// Position in array to start read
        /// </param>
        /// <param name="len">
        /// The length to read
        /// </param>
        /// <returns>
        /// The calculated nuber
        /// </returns>
        private static uint CvFrom64(char[] str, int start, int len)
        {
            int i;
            uint res = 0;

            for (i = 0; i < len; i++)
            {
                int index = -1;
                int j;
                for (j = 0; j < 64; j++)
                {
                    if (Base64Chars[j] == str[start + i])
                    {
                        index = j;
                        break;
                    }
                }

                res = (res * 64) + ((uint)index);
            }

            return res;
        }

        /// <summary>
        /// Conversion of an integer into a characters with base 64
        /// using the table Base64Chars
        /// </summary>
        /// <param name="number">
        /// The number to convert
        /// </param>
        /// <param name="result">
        /// The result char array to write to
        /// </param>
        /// <param name="start">
        /// The position in the char array to start writing
        /// </param>
        /// <param name="len">
        /// The length to write
        /// </param>
        private static void CvTo64(uint number, ref char[] result, int start, int len)
        {
            int digit;

            uint act = number;
            int digits = len;

            for (digit = 0; digit < digits; digit++)
            {
                result[start + len - digit - 1] = Base64Chars[(int)(act % 64)];
                act /= 64;
            }
        }

        /// <summary>
        /// Reconstruction of the GUID from an IFC GUID string (base64)
        /// </summary>
        /// <param name="guid">
        /// The GUID string to convert. Must be 22 characters long
        /// </param>
        /// <returns>
        /// GUID correspondig to the string
        /// </returns>
        public static Guid FromIfcGuid(string guid)
        {
            var num = new uint[6];
            char[] str = guid.ToCharArray();
            int n = 2, pos = 0, i;
            for (i = 0; i < 6; i++)
            {
                num[i] = CvFrom64(str, pos, n);
                pos += n;
                n = 4;
            }

            var a = (int)((num[0] * 16777216) + num[1]);
            var b = (short)(num[2] / 256);
            var c = (short)(((num[2] % 256) * 256) + (num[3] / 65536));
            var d = new byte[8];
            d[0] = Convert.ToByte((num[3] / 256) % 256);
            d[1] = Convert.ToByte(num[3] % 256);
            d[2] = Convert.ToByte(num[4] / 65536);
            d[3] = Convert.ToByte((num[4] / 256) % 256);
            d[4] = Convert.ToByte(num[4] % 256);
            d[5] = Convert.ToByte(num[5] / 65536);
            d[6] = Convert.ToByte((num[5] / 256) % 256);
            d[7] = Convert.ToByte(num[5] % 256);

            return new Guid(a, b, c, d);
        }

        public static string ToIfcGuid(Guid guid)
        {
            var num = new uint[6];
            var str = new char[22];
            byte[] b = guid.ToByteArray();

            // Creation of six 32 Bit integers from the components of the GUID structure
            num[0] = BitConverter.ToUInt32(b, 0) / 16777216;
            num[1] = BitConverter.ToUInt32(b, 0) % 16777216;
            num[2] = (uint)((BitConverter.ToUInt16(b, 4) * 256) + (BitConverter.ToUInt16(b, 6) / 256));
            num[3] = (uint)(((BitConverter.ToUInt16(b, 6) % 256) * 65536) + (b[8] * 256) + b[9]);
            num[4] = (uint)((b[10] * 65536) + (b[11] * 256) + b[12]);
            num[5] = (uint)((b[13] * 65536) + (b[14] * 256) + b[15]);

            // Conversion of the numbers into a system using a base of 64
            int n = 2;
            int pos = 0;
            for (int i = 0; i < 6; i++)
            {
                CvTo64(num[i], ref str, pos, n);
                pos += n;
                n = 4;
            }

            return new string(str);
        }

        public static IfcGuid NewIfcGuid()
        {
            return new IfcGuid();
        }

        public string IfcUuid { get; }
        public Guid NetGuid { get; }

        public IfcGuid() 
            : this(Guid.NewGuid()) { }

        public IfcGuid(Guid guid)
        {
            NetGuid = guid;
            IfcUuid = ToIfcGuid(guid);
        }

        public override string ToString()
        {
            return IfcUuid;
        }
    }
}
