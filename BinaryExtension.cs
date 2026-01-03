namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    public static class BinaryExtension
    {
        public static void Read<T>(this BinaryReader br,  ref T target) where T : IBinaryReadable
        {
            target.Read(br);
        }

        public static void Read<T>(this BinaryReader br, ref T[] targets) where T : IBinaryReadable, new()
        {
            ushort length = (ushort)br.ReadUInt16();
            targets = new T[length];
            for (int i = 0; i < length; i++) {
                targets[i] ??= new T();
                ref T bin = ref targets[i];
                bin.Read(br);
            }
        }
        public static void Read<T>(this BinaryReader br, List<T> targets) where T : IBinaryReadable, new()
        {
            targets.Clear();
            T[] arr = null;
            br.Read(ref arr);
            targets.AddRange(arr);
        }


        public static void Read<T>(this BinaryReader br, byte version, ref T target) where T : IBinaryReadableVersionable
        {
            target.Read(version, br);
        }

        public static void Read<T>(this BinaryReader br, byte version, ref T[] targets) where T : IBinaryReadableVersionable, new()
        {
            ushort length = (ushort)br.ReadUInt16();
            targets = new T[length];
            for (int i = 0; i < length; i++) {
                targets[i] ??= new T();
                ref T bin = ref targets[i];
                bin.Read(version,br);
            }
        }

        public static void Read<T>(this BinaryReader br, byte version, List<T> targets) where T : IBinaryReadableVersionable, new()
        {
            targets.Clear();
            T[] arr = null;
            br.Read(version, ref arr);
            targets.AddRange(arr);
        }

        public static void Write<T>(this BinaryWriter bw, in T target) where T : IBinaryWriteable
        {
            target.Write(bw);
        }

        public static void Write<T>(this BinaryWriter bw, IReadOnlyList<T> targets) where T : IBinaryWriteable
        {
            ushort length = (ushort)targets.Count;
            bw.Write(length);
            for (int i = 0; i < length; i++) {
                targets[i].Write(bw);
            }
        }

        public static string ReadUInt8PrefixedString(this BinaryReader br)
        {
            byte length = br.ReadByte();
            return br.ReadStringOfLength(length);
        }

        public static string ReadShortPrefixedString(this BinaryReader br)
        {
            ushort length = br.ReadUInt16();
            return br.ReadStringOfLength(length);
        }

        public static string ReadStringOfLength(this BinaryReader br, int length)
        {
            char[] buffer = br.ReadChars(length);
            return new string(buffer);
        }

        public static void WriteShortPrefixedString(this BinaryWriter bw, string str)
        {
            ushort l = (ushort)str.Length;
            bw.Write(l);
            for (int i = 0; i < l; i++) {
                bw.Write(str[i]);
            }
        }

        public static void WriteUInt8PrefixedString(this BinaryWriter bw, string str)
        {
            byte l = (byte)str.Length;
            bw.Write(l);
            for (int i = 0; i < l; i++) {
                bw.Write(str[i]);
            }
        }

        public static Dictionary<ushort, string> ReadStringTable(this BinaryReader br)
        {
            ushort size = br.ReadUInt16();

            Dictionary<ushort, string> table = new(size);

            for (ushort i = 0; i < size; i++) {
                string str = br.ReadUInt8PrefixedString();
                table[i] = str;
            }

            return table;
        }

        public static void WriteBytes(this BinaryWriter bw, byte[] arr)
        {
            bw.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++) {
                bw.Write(arr[i]);
            }
        }

        public static byte[] ReadBytes(this BinaryReader br)
        {
            byte[] arr = new byte[br.ReadInt32()];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = br.ReadByte();
            }

            return arr;
        }

        public static void WriteIntegers(this BinaryWriter bw, int[] arr)
        {
            bw.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++) {
                bw.Write(arr[i]);
            }
        }

        public static int[] ReadIntegers(this BinaryReader br)
        {
            int[] arr = new int[br.ReadInt32()];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = br.ReadInt32();
            }

            return arr;
        }

        public static void WriteEnums<T>(this BinaryWriter bw, ICollection<T> enums) where T : System.Enum, IConvertible
        {
            bw.Write(enums.Count);
            T[] arr = new T[enums.Count];
            enums.CopyTo(arr, 0);

            for (int i = 0; i < arr.Length; i++) {
                bw.Write(Convert.ToInt32(arr[i]));
            }
        }

        public static T[] ReadEnums<T>(this BinaryReader br) where T : System.Enum, IConvertible
        {
            int count = br.ReadInt32();
            T[] arr = new T[count];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = (T)(object)br.ReadInt32();
            }

            return arr;
        }


        private static void WriteFloats(this BinaryWriter bw, params float[] floats)
        {
            for (int i = 0; i < floats.Length; i++) {
                bw.Write(floats[i]);
            }
        }

        private static float[] ReadFloats(this BinaryReader br, byte count)
        {
            float[] floats = new float[count];
            for (int i = 0; i < count; i++) {
                floats[i] = br.ReadSingle();
            }

            return floats;
        }

    }
}