
namespace LouveSystems.K2.Lib
{
    using System;

    public interface ITransformEffect
    {
        public struct ConquestEffect : ITransformEffect
        {
            //public bool IsFactionFeat => factionHighlights != EFactionFlag.None;

            public bool Success => newOwningRealm != previousOwningRealm;

            public byte attackingRealm;
            public int regionIndex;
            public int[] attackingRegionsIndices;
            public byte previousOwningRealm;
            public byte newOwningRealm;
            public EFactionFlag factionHighlights;
            public bool isACoinFlip;
            public byte[] otherMajorityAttackersWhoLostCoinFlip;
            public int silverLooted;
            public bool hadBuilding;

            public override string ToString()
            {
                return $"{GetType()} by realm {attackingRealm} on region {regionIndex} from regions [{string.Join(", ", attackingRegionsIndices)}], changing ownership from {previousOwningRealm} to {newOwningRealm} - coin flip? {isACoinFlip}, to whom? [{string.Join(", ", otherMajorityAttackersWhoLostCoinFlip)}]. Region had building? {hadBuilding}. Looted anything? {silverLooted} silver.";
            }

            public void Apply(in GameState previous, ref GameState next)
            {
                if (Success) {

                    next.world.Modify(out Region[] regions, out Realm[] realms);

                    if (regions[regionIndex].buildings == EBuilding.Capital) {
                        // Do not modify
                        return;
                    }

                    regions[regionIndex].ownerIndex = newOwningRealm;
                    regions[regionIndex].isOwned = true;


                    if (previous.world.GetRealmFaction(newOwningRealm).HasFlagSafe(EFactionFlag.ConquestBuilding)) {
                        // Keep building
                    }
                    else {
                        regions[regionIndex].buildings = EBuilding.None;
                    }
                }
            }

            public byte GetNewOwnerFactionIndex(in GameState state)
            {
                return state.world.Realms[newOwningRealm].factionIndex;
            }

            public bool IsFactionFeatForAttacker(in GameState state)
            {
                return IsFactionFeatFor(state.world.GetRealmFaction(attackingRealm));
            }

            public bool IsFactionFeatForNewOwner(in GameState state)
            {
                return 
                    newOwningRealm != byte.MaxValue &&
                    IsFactionFeatFor(state.world.GetRealmFaction(newOwningRealm));
            }

            public bool IsFactionFeatFor(EFactionFlag flag)
            {
                if (flag == EFactionFlag.None) {
                    return false;
                }

                return this.factionHighlights.HasFlagSafe(flag);
            }
        }

        public struct StarvationEffect : ITransformEffect
        {
            public int regionIndex;
            public byte newOwningRealm;
            public bool hasNewOwner;
            public byte waveIndex;
            public bool wasCoinFlip;

            public void Apply(in GameState previous, ref GameState next)
            {
                next.world.Modify(out Region[] regions, out Realm[] realms);
                regions[regionIndex].isOwned = hasNewOwner;

                if (previous.world.GetRealmFaction(newOwningRealm).HasFlagSafe(EFactionFlag.ConquestBuilding) ||
                    !previous.rules.goTakeDestroysBuildings) {
                    // Keep building
                }
                else {
                    regions[regionIndex].buildings = EBuilding.None;
                }

                if (hasNewOwner) {
                    regions[regionIndex].ownerIndex = newOwningRealm;
                }
            }
        }

        public struct ConstructionEffect : ITransformEffect
        {
            public bool IsFactionFeat => isFactionHighlight;

            public int regionIndex;
            public EBuilding building;
            public byte forOwner;
            public int silverPricePaid;
            public bool isFactionHighlight;

            public bool WasSuccessful(in GameState currentGameState)
            {
                return currentGameState.world.IsActionableRegion(forOwner, regionIndex);
            }

            public void Apply(in GameState previous, ref GameState next)
            {
                // ownership check
                if (!next.world.Regions[regionIndex].isOwned) {
                    return;
                }

                byte targetOwner = forOwner;
                {
                    if (next.world.Realms[targetOwner].IsSubjugated(out byte subjugator)) {
                        targetOwner = subjugator;
                    }
                }

                byte regionOwner = next.world.Regions[regionIndex].ownerIndex;
                {
                    if (next.world.Realms[regionOwner].IsSubjugated(out byte subjugator)) {
                        regionOwner = subjugator;
                    }
                }

                if (targetOwner != regionOwner) {
                    return;
                }

                if (next.world.IsActionableRegion(forOwner, regionIndex)) {

                    next.world.Modify(out Region[] regions, out Realm[] realms);

                    regions[regionIndex].buildings |= building;

                    next.world.AddSilverTreasury(regionOwner, -silverPricePaid);
                }
            }

            public byte GetOwnerFactionIndex(in GameState state)
            {
                return state.world.Realms[forOwner].factionIndex;
            }
        }

        public struct FavourPaymentEffect : ITransformEffect
        {
            public byte realmIndex;
            public int silverPricePaid;

            public void Apply(in GameState previous, ref GameState next)
            {
                next.world.Modify(out Region[] regions, out Realm[] realms);

                realms[realmIndex].isFavoured = true;
                next.world.AddSilverTreasury(realmIndex, -silverPricePaid);
            }
        }

        public struct AdministrationUpgradeEffect : ITransformEffect
        {
            public byte realmIndex;
            public int silverPricePaid;

            public void Apply(in GameState previous, ref GameState next)
            {
                next.world.Modify(out Region[] regions, out Realm[] realms);
                realms[realmIndex].availableDecisions = realms[realmIndex].availableDecisions + 1;
                next.world.AddSilverTreasury(realmIndex, -silverPricePaid);
            }
        }

        public struct SubjugationEffect : ITransformEffect
        {
            public byte attackingRealmIndex;
            public byte targetRealmIndex;
            public bool isFactionHighlight;
            public byte attacksBefore;
            public byte attacksAfter;
            public bool subjugationCompleted;

            public void Apply(in GameState previous, ref GameState next)
            {
                if (attackingRealmIndex == targetRealmIndex) {
                    return; /// Should never happen
                }

                attacksBefore = previous.world.Realms[targetRealmIndex].GetSubjugatingAttacksReceived(attackingRealmIndex);
                subjugationCompleted = next.world.AttemptSubjugation(attackingRealmIndex, targetRealmIndex);
                attacksAfter = next.world.Realms[targetRealmIndex].GetSubjugatingAttacksReceived(attackingRealmIndex);
            }
        }

        public struct PartialSubjugationEvolutionEffect : ITransformEffect
        {
            public bool Stalled => attacksBefore == attacksAfter;

            public byte attackingRealmIndex;
            public byte targetRealmIndex;

            public bool LinkedToAttacker => linkedAttackerRegions?.Length > 0;

            public int[] linkedAttackerRegions;

            public byte attacksBefore;
            public byte attacksAfter;

            public void Apply(in GameState previous, ref GameState next)
            {
                attacksBefore = previous.world.Realms[targetRealmIndex].GetSubjugatingAttacksReceived(attackingRealmIndex);

                if (LinkedToAttacker) {
                    // Do nothing
                }
                else {
                    next.world.Modify(out Region[] regions, out Realm[] realms);
                    realms[targetRealmIndex].RemoveSubjugatingAttackFrom(attackingRealmIndex);
                }

                attacksAfter = next.world.Realms[targetRealmIndex].GetSubjugatingAttacksReceived(attackingRealmIndex);
            }
        }

        public bool IsFactionFeat => false;

        public void Apply(in GameState previous, ref GameState next);
    }
}