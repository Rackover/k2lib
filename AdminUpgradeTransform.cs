
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public class AdminUpgradeTransform : Transform
    {
        public override ETransformKind Kind => ETransformKind.ImproveAdministration;

        public override int SilverCost => silverPricePaid;

        public byte realmToUpgrade;
        public int silverPricePaid;

        public AdminUpgradeTransform(byte realmToUpgrade, int silverPricePaid, byte ownerPlayer) : base(ownerPlayer)
        {
            this.realmToUpgrade = realmToUpgrade;
            this.silverPricePaid = silverPricePaid;
        }

        public AdminUpgradeTransform() { }

        public override string ToString()
        {
            return $"{GetType()} on realm {realmToUpgrade} for {SilverCost} silver coins";
        }

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            realmToUpgrade = from.ReadByte();
            silverPricePaid = from.ReadInt32();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(realmToUpgrade);
            into.Write(silverPricePaid);
        }
    }
}