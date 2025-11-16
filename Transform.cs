
namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections.Generic;
    using System.IO;


    [System.Serializable]
    public abstract class Transform : IBinarySerializableWithVersion
    {
        public enum ETransformKind
        {
            Invalid,
            RegionAttack,
            RegionBuild,
            ImproveAdministration,
            PayFavours
        }

        public byte owningRealm;

        public int UniqueID => uniqueId;

        public virtual int DecisionCost => 1;

        public virtual int SilverCost => 0;

        public abstract ETransformKind Kind { get; }

        private int uniqueId;

        protected Transform() { }

        public static Transform Make(ETransformKind kind)
        {
            switch (kind) {
                case ETransformKind.RegionAttack:
                    return new RegionAttackRegionTransform();

                case ETransformKind.ImproveAdministration:
                    return new AdminUpgradeTransform();

                case ETransformKind.PayFavours: 
                    return new PayFavoursTransform();

                case ETransformKind.RegionBuild:
                    return new RegionBuildTransform();
            }

            throw new System.Exception($"Unkown {kind}");
        }

        public Transform(byte owningRealm)
        {
            this.owningRealm = owningRealm;

            unchecked {
                this.uniqueId = Extensions.Hash((int)DateTime.Now.Ticks, owningRealm);
            }
        }

        public void Read(byte version, BinaryReader from)
        {
            this.uniqueId = from.ReadInt32();
            owningRealm = from.ReadByte();
            ReadInternal(from);
        }

        public void Write(BinaryWriter into)
        {
            into.Write(uniqueId);
            into.Write(owningRealm);
            WriteInternal(into);
        }

        public virtual bool CompatibleWith(GameSession existingTransforms) {
            return true;
        }

        protected virtual void ReadInternal(BinaryReader from)
        {

        }

        protected virtual void WriteInternal(BinaryWriter into)
        {

        }

    }
}