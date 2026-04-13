
namespace LouveSystems.K2.Lib
{
    public readonly struct AttackTarget
    {
        public readonly ERegionAttackType attackType;
        public readonly int regionIndex;

        public AttackTarget( int regionIndex, ERegionAttackType attackType)
        {
            this.attackType = attackType;
            this.regionIndex = regionIndex;
        }
    }
}