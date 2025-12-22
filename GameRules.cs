
namespace LouveSystems.K2.Lib
{
    using System.IO;

    [System.Serializable]
    public class GameRules : IBinarySerializableWithVersion
    {
        public const byte VERSION = 4;

        [System.Serializable]
        public struct GlobalFactionSettings : IBinarySerializableWithVersion
        {
            [System.Serializable]
            public struct FactionSettings : IBinarySerializableWithVersion
            {
                public bool enabled;
                public EFactionFlag flag;

                public void Read(byte version, BinaryReader from)
                {
                    enabled = from.ReadBoolean();
                    flag = (EFactionFlag)from.ReadUInt32();
                }

                public void Write(BinaryWriter into)
                {
                    into.Write(enabled);
                    into.Write((uint)flag);
                }
            }

            public byte FactionCount => (byte)factionFlags.Length;

            public byte richesSilverMultiplier; // 2
            public byte richesBuildingMultiplier; // 3
            public byte richesBuildingDivider; // 2

            public byte looterRichesMultiplier;
            public byte looterMinimumSilver;

            public byte conqueredFortPayout;

            public FactionSettings[] factionFlags;

            public void Read(byte version, BinaryReader from)
            {
                if (version <= 3) {
                    factionFlags = new FactionSettings[from.ReadByte()];

                    for (int i = 0; i < factionFlags.Length; i++) {
                        factionFlags[i] = new FactionSettings() {
                            enabled = true,
                            flag = (EFactionFlag)from.ReadUInt16()
                        };
                    }
                }
                else {
                    from.Read(version, ref factionFlags);
                }

                richesSilverMultiplier = from.ReadByte();
                richesBuildingMultiplier = from.ReadByte();
                richesBuildingDivider = from.ReadByte();

                looterRichesMultiplier = from.ReadByte();
                looterMinimumSilver = from.ReadByte();

                conqueredFortPayout = from.ReadByte();
            }

            public void Write(BinaryWriter into)
            {
                into.Write(factionFlags);

                into.Write(richesSilverMultiplier); 
                into.Write(richesBuildingMultiplier); 
                into.Write(richesBuildingDivider);

                into.Write(looterRichesMultiplier);
                into.Write(looterMinimumSilver);

                into.Write(conqueredFortPayout);
            }
        }


        [System.Serializable]
        public struct BuildingSettings : IBinarySerializableWithVersion
        {
            public EBuilding building;

            public byte silverRevenue;

            public byte silverCost;

            public bool canBeBuilt;

            public void Read(byte version, BinaryReader from)
            {
                building = (EBuilding)from.ReadByte();
                silverRevenue = from.ReadByte();
                silverCost = from.ReadByte();
                canBeBuilt = from.ReadBoolean();
            }

            public void Write(BinaryWriter into)
            {
                into.Write((byte)building);
                into.Write(silverRevenue);
                into.Write(silverCost);
                into.Write(canBeBuilt);
            }
        }

        [System.Serializable]
        public struct VotingSettings : IBinarySerializableWithVersion
        {
            public EVotingCriteria criteria;
            public bool enabled;
            public byte activeAfterCouncils;
            public byte chancesToBeSelected;
            public byte influenceWeight;

            public void Read(byte version, BinaryReader from)
            {
                criteria = (EVotingCriteria)from.ReadByte();
                enabled = from.ReadBoolean();
                activeAfterCouncils = from.ReadByte();
                chancesToBeSelected = from.ReadByte();
                influenceWeight = from.ReadByte();
            }

            public void Write(BinaryWriter into)
            {
                into.Write((byte)criteria);
                into.Write(enabled);
                into.Write(activeAfterCouncils);
                into.Write(chancesToBeSelected);
                into.Write(influenceWeight);
            }
        }

        [System.Serializable]
        public class VotingRules : IBinarySerializableWithVersion
        {
            public const byte VERSION = 2;

            public int voterCount = 33;

            public byte[] criteriasUsedPerVote = new byte[]{
                byte.MaxValue
            };

            public byte[] turnoverPercentagePerCouncil = new byte[] {
                33,
                51,
                75,
                95,
                100
            };

            public VotingSettings[] votingCriterias;

            public bool forceMajorityEventually = false;

            public void Read(byte version, BinaryReader from)
            {
                version = from.ReadByte();

                voterCount = from.ReadInt32();
                criteriasUsedPerVote = from.ReadBytes();
                turnoverPercentagePerCouncil = from.ReadBytes();

                votingCriterias = new VotingSettings[from.ReadByte()];
                for (int i = 0; i < votingCriterias.Length; i++) {
                    votingCriterias[i].Read(version, from);
                }

                if (version >= 2) {
                    forceMajorityEventually = from.ReadBoolean();
                }
            }

            public void Write(BinaryWriter into)
            {
                into.Write(VERSION);

                into.Write(voterCount);
                into.WriteBytes(criteriasUsedPerVote);
                into.WriteBytes(turnoverPercentagePerCouncil);

                into.Write((byte)votingCriterias.Length);
                for (int i = 0; i < votingCriterias.Length; i++) {
                    votingCriterias[i].Write(into);
                }

                into.Write(forceMajorityEventually);
            }
        }

        public byte additionalRealmsCount = 1;

        public bool hasCouncilRealm = true;

        public byte councilRealmRegionSize = 1;

        public byte initialSafetyMarginBetweenRealms = 1;

        public byte initialRealmsSize = 1;

        public byte silverRevenuePerRegion = 1;

        public byte startingGold = 2;

        public byte startingDecisionCount = 3;

        public byte maxDecisionCount = 5;

        public byte favourGoldPrice = 10;

        public byte enhanceAdminGoldPrice = 2;

        public byte enhanceAdminGoldPriceIncreasePerUpgrade = 10;

        public bool allowLooting = true;

        public byte silverLootedOnCapital = 10;

        public bool neutralRegionStarvation = true;

        public bool goTakeNeutralOnlyWhenNoContest = true;

        public bool goTakeDestroysBuildings = false;

        public byte turnsBetweenVotes = 4;

        public byte initialVoteTurnsDelay = 1;

        public int decisionTimeSeconds = 30;

        public int additionalDecisionTimeSecondsOnFirstTurn = 30;

        public byte eatenCorners = 2;

        public byte eatFirstLastColumns = 1;

        public bool capitalCanReplay = true;

        public bool subjugationForAll = false;

        public VotingRules voting = new VotingRules();

        public BuildingSettings[] buildings = new BuildingSettings[0];

        public GlobalFactionSettings factions = new GlobalFactionSettings();

        public void Write(BinaryWriter into)
        {
            into.Write(VERSION);

            into.Write(initialRealmsSize);
            into.Write(additionalRealmsCount);
            into.Write(councilRealmRegionSize);
            into.Write(hasCouncilRealm);
            into.Write(startingGold);
            into.Write(initialSafetyMarginBetweenRealms);
            into.Write(initialVoteTurnsDelay);
            into.Write(decisionTimeSeconds);
            into.Write(additionalDecisionTimeSecondsOnFirstTurn);
            into.Write(enhanceAdminGoldPrice);
            into.Write(enhanceAdminGoldPriceIncreasePerUpgrade);
            into.Write(favourGoldPrice);

            into.Write(allowLooting);
            into.Write(silverLootedOnCapital);

            into.Write(neutralRegionStarvation);
            into.Write(goTakeNeutralOnlyWhenNoContest);

            into.Write(turnsBetweenVotes);
            into.Write(eatenCorners);
            into.Write(eatFirstLastColumns);
            into.Write(startingDecisionCount); 
            into.Write(maxDecisionCount);

            into.Write(silverRevenuePerRegion);

            into.Write(capitalCanReplay);
            into.Write(subjugationForAll);

            into.Write((byte)buildings.Length);
            for (int i = 0; i < buildings.Length; i++) {
                buildings[i].Write(into);
            }

            voting.Write(into);
            factions.Write(into);

            into.Write(goTakeDestroysBuildings);
        }

        public void Read(byte version, BinaryReader from)
        {
            version = from.ReadByte();

            if (version > VERSION) {
                return;
            }

            initialRealmsSize = from.ReadByte();
            additionalRealmsCount = from.ReadByte();
            councilRealmRegionSize = from.ReadByte();
            hasCouncilRealm = from.ReadBoolean();
            startingGold = from.ReadByte();
            initialSafetyMarginBetweenRealms = from.ReadByte();
            initialVoteTurnsDelay = from.ReadByte();
            decisionTimeSeconds = from.ReadInt32();
            additionalDecisionTimeSecondsOnFirstTurn = from.ReadInt32(); 
            enhanceAdminGoldPrice = from.ReadByte();
            enhanceAdminGoldPriceIncreasePerUpgrade = from.ReadByte();
            favourGoldPrice = from.ReadByte();

            allowLooting = from.ReadBoolean();
            silverLootedOnCapital = from.ReadByte();

            neutralRegionStarvation = from.ReadBoolean();
            goTakeNeutralOnlyWhenNoContest = from.ReadBoolean();

            turnsBetweenVotes = from.ReadByte();
            eatenCorners = from.ReadByte();
            eatFirstLastColumns = from.ReadByte();
            startingDecisionCount = from.ReadByte();
            maxDecisionCount = from.ReadByte();

            silverRevenuePerRegion = from.ReadByte();

            capitalCanReplay = from.ReadBoolean();
            subjugationForAll = from.ReadBoolean();

            buildings = new BuildingSettings[from.ReadByte()];
            for (int i = 0; i < buildings.Length; i++) {
                buildings[i].Read(version, from);
            }

            voting.Read(version, from);
            factions.Read(version, from);

            if (version >= 3) {
                goTakeDestroysBuildings = from.ReadBoolean();
            }
        }

        public GameRules Duplicate()
        {
            using (MemoryStream ms = new MemoryStream()) {
                using(BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) {
                    Write(bw);
                }

                ms.Seek(0, SeekOrigin.Begin);

                var dupe = new GameRules();

                using (BinaryReader br = new BinaryReader(ms)) {
                    dupe.Read(default, br);
                }

                return dupe;
            }
        }

        public BuildingSettings GetBuilding(EBuilding building)
        {
            for (int i = 0; i < buildings.Length; i++) {
                if (buildings[i].building == building) {
                    return buildings[i];
                }
            }

            throw new System.Exception($"Invalid building {building}");
        }

        public VotingSettings GetVotingSetting(EVotingCriteria criteria)
        {
            for (int i = 0; i < voting.votingCriterias.Length; i++) {
                if (criteria == voting.votingCriterias[i].criteria) {
                    return voting.votingCriterias[i];
                }
            }

            throw new System.Exception($"Invalid criteria {criteria}");
        }

        public override bool Equals(object obj)
        {
            return obj is GameRules &&
                obj is IHashable hashable && 
                hashable.GetHash() == (this as IHashable).GetHash();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}