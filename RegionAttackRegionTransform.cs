
namespace LouveSystems.K2.Lib
{
    using System.Collections.Generic;
    using System.IO;

    public class RegionAttackRegionTransform : RegionRelatedTransform
    {
        public int AttackingRegionIndex => actingRegionIndex;

        public override int DecisionCost => attackType != ERegionAttackType.Charge ? 1 : 0;

        public int targetRegionIndex;
    
        public ERegionAttackType attackType;

        public RegionAttackRegionTransform() : base() { }

        public RegionAttackRegionTransform(int attackingRegionIndex, int targetRegionIndex, ERegionAttackType attackType, byte owningRealm) : base(attackingRegionIndex, owningRealm)
        {
            this.targetRegionIndex = targetRegionIndex;
            this.attackType = attackType;
        }

        public override ETransformKind Kind => ETransformKind.RegionAttack;

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            targetRegionIndex = from.ReadInt32();
            attackType = (ERegionAttackType)from.ReadByte();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(targetRegionIndex);
            into.Write((byte)attackType);
        }

        public override string ToString()
        {
            return $"Region attack {attackType} {AttackingRegionIndex} => {targetRegionIndex}";
        }
    }
}