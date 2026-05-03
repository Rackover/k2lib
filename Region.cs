
namespace LouveSystems.K2.Lib
{
    using System.IO;

    public struct Region : IBinarySerializableWithVersion
    {
        public EBuilding Building {get { return buildings; } set { buildings = value; } }
        public EBuilding PretensedBuilding => buildings.GetPretensedBuilding();
        public EBuilding RelevantBuilding => buildings.GetBuildingOrRawDecoy();

        public bool inert;
        public bool isOwned;
        public byte ownerIndex;

        private EBuilding buildings;

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
            return isOwned && RelevantBuilding.HasFlagSafe(EBuilding.Capital) && rules.capitalCanReplay;
        }

        public bool CannotBeTaken(GameRules rules, EFactionFlag byFaction)
        {
            if (inert) {
                return true;
            }

            if (isOwned) {
                if (RelevantBuilding.HasFlagSafe(EBuilding.Capital)) {
                    return !rules.subjugationForAll && !byFaction.HasFlagSafe(EFactionFlag.Subjugate);
                }
            }

            return false;
        }

        public bool IsReinforcedAgainstAttack(GameRules rules, EFactionFlag byFaction)
        {
            if ((rules.subjugationForAll || byFaction.HasFlagSafe(EFactionFlag.Subjugate))
                && RelevantBuilding.HasFlagSafe(EBuilding.Capital)) {
                return true;
            }

            return RelevantBuilding.HasFlagSafe(EBuilding.Fort);
        }

        public int GetSilverWorth(EFactionFlag faction, GameRules rules)
        {
            int revenue;

            revenue = rules.silverRevenuePerRegion;
            if (faction.HasFlagSafe(EFactionFlag.RicherTerritories)) {
                revenue *= rules.factions.richesSilverMultiplier;
            }

            if (RelevantBuilding != EBuilding.None) {

                var buildingRule = rules.GetBuilding(RelevantBuilding);

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
            return $"Region {(inert ? "inert " : string.Empty)}with {buildings} ({(isOwned ? $"owned by realm {ownerIndex}" : "free of ownership")})";
        }
    }
}