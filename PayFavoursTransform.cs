
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public class PayFavoursTransform : Transform
    {
        public override ETransformKind Kind => ETransformKind.PayFavours;

        public override int SilverCost => silverPricePaid;

        public byte realmToFavour;
        public int silverPricePaid;

        public PayFavoursTransform(byte realmToUpgrade, int silverPaidPrice, byte ownerPlayer) : base(ownerPlayer)
        {
            this.realmToFavour = realmToUpgrade;
            this.silverPricePaid = silverPaidPrice;
        }

        public PayFavoursTransform() { }

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            realmToFavour = from.ReadByte();
            silverPricePaid = from.ReadInt32();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(realmToFavour);
            into.Write(silverPricePaid);
        }
        public override bool CompatibleWith(GameSession session)
        {
            if (!session.GetOwnerOfRealm(realmToFavour, out byte owningPlayerId, subjugator: false) ||
                !session.SessionPlayers[owningPlayerId].CanPayForFavours()) {
                return false;
            }

            return base.CompatibleWith(session); 
        }
    }
}