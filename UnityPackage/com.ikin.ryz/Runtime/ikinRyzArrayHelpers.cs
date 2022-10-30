using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// A collection of utility functions for working with arrays.
    /// </summary>
    /// <remarks>
    /// The goal of this collection is to make it easy to use arrays directly rather than resorting to
    /// <see cref="List{T}"/>.
    /// </remarks>
    internal static class ikinRyzArrayHelpers
    {
        public static void Clear<TValue>(this TValue[] array)
        {
            if (array == null)
            {
                return;
            }

            Array.Clear(array, 0, array.Length);
        }

        public static void Clear<TValue>(this TValue[] array, int count)
        {
            if (array == null)
            {
                return;
            }

            Array.Clear(array, 0, count);
        }

        public static bool ContainsReference<TValue>(TValue[] array, int count, TValue value)
            where TValue : class
        {
            return IndexOfReference(array, value, count) != -1;
        }

        public static int IndexOfReference<TValue>(this TValue[] array, TValue value, int count = -1)
            where TValue : class
        {
            return IndexOfReference(array, value, 0, count);
        }

        public static int IndexOfReference<TValue>(this TValue[] array, TValue value, int startIndex, int count)
            where TValue : class
        {
            if (array == null)
                return -1;

            if (count < 0)
                count = array.Length - startIndex;
            for (var i = startIndex; i < startIndex + count; ++i)
                if (ReferenceEquals(array[i], value))
                    return i;

            return -1;
        }

        public static int AppendWithCapacity<TValue>(ref TValue[] array, ref int count, TValue value, int capacityIncrement = 10)
        {
            if (array == null)
            {
                array = new TValue[capacityIncrement];
                array[0] = value;
                ++count;
                return 0;
            }

            var capacity = array.Length;
            if (capacity == count)
            {
                capacity += capacityIncrement;
                Array.Resize(ref array, capacity);
            }

            var index = count;
            array[index] = value;
            ++count;

            return index;
        }

        public static void EraseAtWithCapacity<TValue>(TValue[] array, ref int count, int index)
        {
            Debug.Assert(array != null);
            Debug.Assert(count <= array.Length);
            Debug.Assert(index >= 0 && index < count);

            // If we're erasing from the beginning or somewhere in the middle, move
            // the array contents down from after the index.
            if (index < count - 1)
            {
                Array.Copy(array, index + 1, array, index, count - index - 1);
            }

            array[count - 1] = default; // Tail has been moved down by one.
            --count;
        }
    }