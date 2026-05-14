
namespace LouveSystems.K2.Lib
{

    public interface IBoard : IBinarySerializable
    {
        public EBoardType Type { get; }

        public IBoard Duplicate();

        public void ComputeEffects(ManagedRandom random, in GameState state, out ITransformEffect[] effects);

        public static IBoard CreateBoard(EBoardType type, in World world)
        {
            switch(type) {
                default:
                    return new EmptyBoard(type);
            }
        }
    }
}