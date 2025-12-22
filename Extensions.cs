
namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections.Generic;

    public static class Extensions
    {
        public static bool IsDe(this char c)
        {
            c = char.ToLowerInvariant(c);
            return !c.IsVowel();
        }

        public static bool IsVowel(this char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'a' || c == 'e' || c == 'i' || c == 'u' || c == 'o' || c == 'y' ||
                c == 'ô' || c == 'é' || c == 'è' || c == 'ù' || c == 'ë' || c == '^';
        }

        public static bool HasFlagSafe<T>(this T value, T flag) where T : Enum
        {
            return value.HasFlag(flag);
        }

        public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
        {
            for (int i = 0; i < list.Count; i++) {
                action(list[i]);
            }
        }

        public static List<T> FindAll<T>(this IReadOnlyList<T> list, Predicate<T> action)
        {
            List<T> results = new List<T>();
            for (int i = 0; i < list.Count; i++) {
                if (action(list[i])) {
                    results.Add(list[i]);
                }
            }

            return results;
        }


        public static T Find<T>(this IReadOnlyList<T> list, Predicate<T> action)
        {
            for (int i = 0; i < list.Count; i++) {
                if (action(list[i])) {
                    return list[i];
                }
            }

            return default;
        }

        public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> action)
        {
            for (int i = 0; i < list.Count; i++) {
                if (action(list[i])) {
                    return i;
                }
            }

            return -1;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Math.Abs(list.GetHashCode()) % n;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void Shuffle<T>(this IList<T> list, ManagedRandom random)
        {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = random.Next(n);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static int MultiplyByPercentage(this int integer, int percentage0100)
        {
            integer *= percentage0100;

            return integer / 100;
        }

        public static int Hash<T>(params T[][] hashables) where T : IHashable
        {
            int[] hashes = new int[hashables.Length];
            for (int i = 0; i < hashables.Length; i++) {
                hashes[i] = Hash(hashables[i]);
            }

            return Hash(hashes);
        }

        public static int Hash<T>(params T[] hashables) where T : IHashable
        {
            int hash = 17;

            void addToHash(int i)
            {
                unchecked // Overflow is fine, just wrap
                {
                    hash = hash * 23 + i;
                }
            }

            for (int i = 0; i < hashables.Length; i++) {
                int hashResult = hashables[i].GetHash();
                Logger.Trace($"Hash of {hashables[i]} is {hashResult:X8}");
                addToHash(hashResult);
            }

            return hash;
        }

        public static int Hash(params byte[] integers)
        {
            int hash = 17;

            void addToHash(int i)
            {
                unchecked // Overflow is fine, just wrap
                {
                    hash = hash * 23 + i;
                }
            }

            for (int i = 0; i < integers.Length; i++) {
                addToHash(integers[i]);
            }

            return hash;
        }

        public static int Hash(params int[] integers)
        {
            int hash = 17;

            void addToHash(int i)
            {
                unchecked // Overflow is fine, just wrap
                {
                    hash = hash * 23 + i;
                }
            }

            for (int i = 0; i < integers.Length; i++) {
                addToHash(integers[i]);
            }

            return hash;
        }

        public static void AddRange<T>(this ICollection<T> collection, IReadOnlyCollection<T> toAdd)
        {
            foreach(var elem in toAdd) {
                collection.Add(elem);
            }
        }
    }
}