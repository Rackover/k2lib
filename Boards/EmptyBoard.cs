
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public struct EmptyBoard : IBoard
    {
        public EBoardType Type { get; }

        public EmptyBoard(EBoardType type)
        {
            this.Type = type;
        }

        public IBoard Duplicate()
        {
            return new EmptyBoard(Type);
        }

        public void ComputeEffects(ManagedRandom random, in GameState state, out ITransformEffect[] effects)
        {
            effects = new ITransformEffect[0];
        }

        public void Write(BinaryWriter into)
        {
        }

        public void Read(BinaryReader from)
        {
        }
    }
}