using System;

namespace CoreBoy.Core.Utils
{
    public static class ExtensionMethods
    {
        public static bool GetBit(this byte target, int bitIndex)
        {
            return (target & (1 << bitIndex)) != 0;
        }

        public static byte SetBit(this byte target, int bitIndex, bool value)
        {
            if (value)
            {
                target = (byte)(target | (1 << bitIndex));
            }
            else
            {
                target = (byte)(target & ~(1 << bitIndex));
            }

            return target;
        }

        public static T[] Populate<T>(this T[] array, Func<T> provider)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = provider();
            }
            return array;
        }
    }
}