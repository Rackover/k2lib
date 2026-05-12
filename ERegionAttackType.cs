
namespace LouveSystems.K2.Lib
{
    [System.Flags]
    public enum ERegionAttackType
    {
        Standard = 0,
        Charge = 1 << 0,
        Slithering = 1 << 2,

        All = -1
    }
}