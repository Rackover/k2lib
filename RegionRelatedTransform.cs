
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public abstract class RegionRelatedTransform : Transform
    {
        public int actingRegionIndex;

        public RegionRelatedTransform() : base() { }

        public RegionRelatedTransform(int actingRegionIndex, byte owningRealm) : base(owningRealm)
        {
            this.actingRegionIndex = actingRegionIndex;
        }

        protected override void ReadInternal(BinaryReader from)
        {
            base.ReadInternal(from);
            actingRegionIndex = from.ReadInt32();
        }

        protected override void WriteInternal(BinaryWriter into)
        {
            base.WriteInternal(into);
            into.Write(actingRegionIndex);
        }

        public override bool CompatibleWith(GameSession session)
        {
            if (session.HasRegionPlayed(actingRegionIndex, out RegionRelatedTransform otherTransform)) {
                Logger.Trace($"Region {actingRegionIndex} has already played, but...");
                if (session.CurrentGameState.world.Regions[actingRegionIndex].CanReplay(session.Rules)) {
                    // OK
                    Logger.Trace($"... it can replay, so that's OK.");
                }
                else if (this is RegionAttackRegionTransform atk && atk.isExtendedAttack) {
                    Logger.Trace($"... it's an extended attack, so we will allow it.");
                }
                else if (otherTransform is RegionAttackRegionTransform otherAtk && otherAtk.isExtendedAttack) {
                    Logger.Trace($"... the previous played transform on this region was an extended attack ({otherAtk}) so we allow it.");
                }
                else {
                    Logger.Trace($"... it's unacceptable! It has at least one other ongoing transform ({otherTransform}), it cannot replay ({session.CurrentGameState.world.Regions[actingRegionIndex]}) and this is not an extended attack!!");
                    Logger.Warn($"Discarding transform {this} because this region cannot replay!");

                    return false;
                }
            }

            return true;
        }
    }
}