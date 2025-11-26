
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public struct Region : IBinarySerializableWithVersion
    {
        public bool inert;
        public bool isOwned;
        public byte ownerIndex;
        public EBuilding buildings;

        public bool GetOwner(out byte realmIndex)
        {
            realmIndex = ownerIndex;
            return isOwned;
        }

        public bool IsOwnedBy(byte realmIndex)
        {
            return isOwned && ownerIndex == realmIndex;
        }

        public bool CanReplay(GameRules rules)
        {
            return isOwned && buildings.HasFlagSafe(EBuilding.Capital) && rules.capitalCanReplay;
        }

        public bool CannotBeTaken(GameRules rules, EFactionFlag byFaction)
        {
            if (inert) {
                return true;
            }

            if (isOwned) {
                if (buildings.HasFlagSafe(EBuilding.Capital)) {
                    return !rules.subjugationForAll && !byFaction.HasFlagSafe(EFactionFlag.Subjugate);
                }
            }

            return false;
        }

        public bool IsReinforcedAgainstAttack(GameRules rules, EFactionFlag byFaction)
        {
            if ((rules.subjugationForAll || byFaction.HasFlagSafe(EFactionFlag.Subjugate))
                && buildings.HasFlagSafe(EBuilding.Capital)) {
                return true;
            }

            return buildings.HasFlagSafe(EBuilding.Fort);
        }

        public int GetSilverWorth(EFactionFlag faction, GameRules rules)
        {
            int revenue;

            revenue = rules.silverRevenuePerRegion;
            if (faction.HasFlagSafe(EFactionFlag.RicherTerritories)) {
                revenue *= rules.factions.richesSilverMultiplier;
            }

            if (buildings != EBuilding.None) {

                var buildingRule = rules.GetBuilding(buildings);

                if (buildingRule.silverRevenue != 0) {
                    revenue = buildingRule.silverRevenue;

                    if (faction.HasFlagSafe(EFactionFlag.RicherTerritories)) {
                        revenue = revenue * rules.factions.richesBuildingMultiplier;
                        revenue = revenue / rules.factions.richesBuildingDivider;
                    }
                }
            }

            return revenue;
        }

        public int GetHash()
        {
            return Extensions.Hash(
                inert ? 1 : 0,
                isOwned ? 1 : 0,
                ownerIndex,
                (int)buildings
            );
        }

        public void Write(BinaryWriter into)
        {
            into.Write(inert);
            into.Write(isOwned);
            into.Write(ownerIndex);
            into.Write((byte)buildings);
        }

        public void Read(byte version, BinaryReader from)
        {
            inert = from.ReadBoolean();
            isOwned = from.ReadBoolean();
            ownerIndex = from.ReadByte();
            buildings = (EBuilding)from.ReadByte();
        }

        public override string ToString()
        {
            return $"Region {(inert ? "inert " : string.Empty)}with {buildings} ({(isOwned ? "free of ownership" : $"owned by realm {ownerIndex}")})";
        }
    }
}