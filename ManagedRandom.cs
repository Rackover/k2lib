
namespace LouveSystems.K2.Lib
{
    using System;
    using System.IO;

    public class ManagedRandom : IBinarySerializable
    {
        private const byte VERSION = 1;

        private const int PRE_ROLLS = 128;

        public int Position { get; private set; }

        private readonly int[] Rolls = new int[PRE_ROLLS];

        public ManagedRandom(int seeds)
        {
            Random random = new Random(seeds);
            for (int i = 0; i < Rolls.Length; i++) {
                Rolls[i] = random.Next();
            }

            Position = 0;
        }
        
        private ManagedRandom(ManagedRandom other)
        {
            Position = other.Position;
            other.Rolls.CopyTo(Rolls, 0);
        }


        public int Next()
        {
            Position = (Position+1)%Rolls.Length;
            int val = Rolls[Position];

            Logger.Trace($"Rolled a {val} ({Position} rolls)");

            return val;
        }

        public int Next(int max)
        {
            return Next() % max;
        }

        public int GetHash()
        {
            return
                Extensions.Hash(
                    Position,
                    Extensions.Hash(Rolls)
                );
        }

        public override string ToString()
        {
            return $"Managed Random ({Position} rolls)";
        }

        public void Write(BinaryWriter into)
        {
            into.Write(VERSION);
            into.Write(Position);

            for (int i = 0; i < Rolls.Length; i++) {
                into.Write(Rolls[i]);
            }
        }

        public void Read( BinaryReader from)
        {
            byte version = from.ReadByte();
            Position = from.ReadInt32();
        }

        public ManagedRandom Duplicate()
        {
            ManagedRandom dupe = new ManagedRandom(this);
            return dupe;
        }
    }
}