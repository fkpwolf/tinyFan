using System;
using System.Collections.Generic;

namespace utilities
{
    internal static class utilities
    {
        public static byte[] StrToByteArray(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        public static string ByteArrayToString(byte[] b, int firstByteIndex)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(b, firstByteIndex, b.Length - firstByteIndex);
        }

        public static string ByteArrayToString(byte[] b, int firstByteIndex, int length)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(b, firstByteIndex, length);
        }

        public static string numToHexString(byte num)
        {
            return String.Format("{00:X}", num);
        }

        public static string numToHexString(ushort num)
        {
            return String.Format("{0:X}", num);
        }

        public static int hexStringToNum(string _0xNUM)
        {
            if (_0xNUM.StartsWith("0x"))
                _0xNUM = _0xNUM.Substring(2);
            int num = Int32.Parse(_0xNUM, System.Globalization.NumberStyles.HexNumber);
            return num;
        }

        /// <summary>
        /// Converts a hex string such as "01 02 03 04" to {0x01, 0x02, 0x04, 0x04}
        /// </summary>
        public static byte[] hexStringToByteArray(string hexString)
        {
            char[] seperators = { ' ' };
            string[] hexNums = hexString.Split(seperators);

            byte[] b = new byte[hexNums.Length];
            for (int i = 0; i < hexNums.Length; i++)
                b[i] = Byte.Parse(hexNums[i], System.Globalization.NumberStyles.HexNumber);
            return b;
        }

        public static string byteArrayToHexString(byte[] indata)
        {
            string s = "";
            foreach (byte b in indata)
                s = s + numToHexString(b) + " ";
            return s;
        }

        public static int convertTwosComplement(UInt16 num)
        {
            return 0;
        }

        public static bool stringIsInteger(string val)
        {
            Double result;
            return Double.TryParse(val, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.CurrentCulture, out result);
        }

        public static int coerce(int value, int max, int min)
        {
            if (value > max)
                value = max;
            if (value < min)
                value = min;
            return value;
        }

        public static double coerce(double value, double max, double min)
        {
            if (value > max)
                value = max;
            if (value < min)
                value = min;
            return value;
        }
    }
}