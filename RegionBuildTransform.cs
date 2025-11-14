
namespace LouveSystems.K2.Lib
{
    using System.Collections.Generic;
    using System.IO;

    public class RegionBuildTransform : RegionRelatedTransform
    {
        public override ETransformKind Kind => ETransformKind.RegionBuild;

        public EBuilding building;

        public byte constructingRealmIndex;

        public int silverCost = 10;

        public override int SilverCost => silverCost;

        public RegionBuildTransform() : base() { }

        public RegionBuildTransform(EBuilding building, int silverCost, byte constructingRealmIndex, int regionIndex, byte owner) : base(regionIndex, owner)
        {
            this.building = building;
            this.silverCost = silverCost;
            this.constructingRealmIndex = constructingRealmIndex;
        }

        public bool IsPrioritized(in World world)
        {
            if (building == EBuilding.Fort &&
                world.GetRegionFaction(this.actingRegionIndex) == EFactionFlag.FortsCountAsCapital) {
                return true;
            }

            return false;
        }

        public override bool CompatibleWith(IReadOnlyList<Transform> existingTransforms)
        {
            for (int i = 0; i < existingTransforms.Count; i++) {
                if (existingTransforms[i] is RegionRelatedTransform otherTransform) {
                    if (otherTransform.actingRegionIndex == actingRegionIndex) {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            building = (EBuilding)from.ReadByte();
            constructingRealmIndex = from.ReadByte();
            silverCost = from.ReadInt32();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write((byte)building);
            into.Write(constructingRealmIndex);
            into.Write(silverCost);
        }

        public override string ToString()
        {
            return $"{Kind} {building} by {constructingRealmIndex} on region {actingRegionIndex} for {silverCost} silver";
        }
    }
}