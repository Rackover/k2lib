
namespace LouveSystems.K2.Lib
{
    public enum EBuilding
    {
        None = 0,
        Capital = 1,
        Fields= 1 << 1,
        Fort = 1 << 2,
        Church = 1 << 3,

        Decoy = 1 << 7
    }
}