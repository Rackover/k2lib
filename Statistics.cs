
namespace LouveSystems.K2.Lib
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public struct Statistics : IBinarySerializableWithVersion
    {
        public struct RealmStatistics : IBinarySerializableWithVersion
        {
            public ushort regionsLostToAttrition;
            public ushort regionsGainedFromAttrition;
            public ushort regionsLostToConquest;
            public ushort regionsGainedFromConquest;
            public ushort maxTerritorySize;
            public ushort minTerritorySize;
            public ushort maxRiches;
            public ushort minRiches;
            public ushort maxAdministration;
            public ushort maxBuildings;
            public ushort buildingsConstructed;
            public ushort buildingsDestroyed;
            public ushort maxVotesInSingleCouncil;
            public ushort silverLooted;
            public ushort tally;
            public uint silverSpent;
            public uint silverGained;

            public Dictionary<EBuilding, ushort> buildingKindsConstructed;

            public void NotifyBuildingConstructed(EBuilding building)
            {
                buildingKindsConstructed ??= new Dictionary<EBuilding, ushort>();
                buildingKindsConstructed.TryAdd(building, 0);
                buildingKindsConstructed[building]++;

                buildingsConstructed++;
            }

            public void Write(BinaryWriter into)
            {
                into.Write(regionsLostToAttrition);
                into.Write(regionsGainedFromAttrition);
                into.Write(regionsLostToConquest);
                into.Write(regionsGainedFromConquest);
                into.Write(maxTerritorySize);
                into.Write(minTerritorySize);
                into.Write(maxRiches);
                into.Write(minRiches);
                into.Write(maxAdministration);
                into.Write(maxBuildings);
                into.Write(buildingsConstructed);
                into.Write(buildingsDestroyed);
                into.Write(tally);
                into.Write(maxVotesInSingleCouncil);
                into.Write(silverLooted);

                into.Write(buildingKindsConstructed?.Count ?? 0);

                if (buildingKindsConstructed != null) {
                    foreach (var kv in buildingKindsConstructed) {
                        into.Write((int)kv.Key);
                        into.Write(kv.Value);
                    }
                }
                into.Write(silverSpent);
                into.Write(silverGained);
            }

            public void Read(byte version, BinaryReader from)
            {
                regionsLostToAttrition = from.ReadUInt16();
                regionsGainedFromAttrition = from.ReadUInt16();
                regionsLostToConquest = from.ReadUInt16();
                regionsGainedFromConquest = from.ReadUInt16();
                maxTerritorySize = from.ReadUInt16();
                minTerritorySize = from.ReadUInt16();
                maxRiches = from.ReadUInt16();
                minRiches = from.ReadUInt16();
                maxAdministration = from.ReadUInt16();
                maxBuildings = from.ReadUInt16();
                buildingsConstructed = from.ReadUInt16();
                buildingsDestroyed = from.ReadUInt16();
                tally = from.ReadUInt16();
                maxVotesInSingleCouncil = from.ReadUInt16();
                silverLooted = from.ReadUInt16();

                int dictionaryLength = from.ReadInt32();
                buildingKindsConstructed ??= new Dictionary<EBuilding, ushort>();
                buildingKindsConstructed.Clear();
                for (int i = 0; i < dictionaryLength; i++) {
                    buildingKindsConstructed[(EBuilding)from.ReadInt32()] = from.ReadUInt16();
                }

                silverSpent = from.ReadUInt32();
                silverGained = from.ReadUInt32();
            }
        }

        public struct RegionStatistics : IBinarySerializableWithVersion
        {
            public ushort conquests;
            public ushort buildingsConstructed;
            public ushort starvations;
            public Dictionary<byte, ushort> conquestsPerRealm;

            public void Read(byte version, BinaryReader from)
            {
                conquests = from.ReadUInt16();
                buildingsConstructed = from.ReadUInt16();
                starvations = from.ReadUInt16();

                int dictionaryLength = from.ReadInt32();
                conquestsPerRealm ??= new Dictionary<byte, ushort>();
                conquestsPerRealm.Clear();
                for (int i = 0; i < dictionaryLength; i++) {
                    byte key = from.ReadByte();
                    ushort value = from.ReadUInt16();
                    conquestsPerRealm[key] = value;
                }
            }

            public void Write(BinaryWriter into)
            {
                into.Write(conquests);
                into.Write(buildingsConstructed);
                into.Write(starvations);

                into.Write(conquestsPerRealm?.Count ?? 0);

                if (conquestsPerRealm != null) {
                    foreach (var kv in conquestsPerRealm) {
                        into.Write(kv.Key);
                        into.Write(kv.Value);
                    }
                }
            }
        }

        public IReadOnlyList<RealmStatistics> Realms => realms;

        public IReadOnlyList<RegionStatistics> Regions => regions;

        public RealmStatistics[] realms;
        public RegionStatistics[] regions;

        public void Write(BinaryWriter into)
        {
            into.Write(realms);
            into.Write(regions);
        }

        public void Read(byte version, BinaryReader from)
        {
            from.Read(version, ref realms);
            from.Read(version, ref regions);
        }

        public Statistics(Statistics other)
        {
            realms = ((IEnumerable<RealmStatistics>)other.realms)
                .ForEach(o =>
                {
                    var copy = o;

                    // copy dictionary
                    copy.buildingKindsConstructed = o.buildingKindsConstructed?.ToDictionary(kv => kv.Key, kv => kv.Value);

                    return copy;
                })
                .ToArray();

            regions = ((IEnumerable<RegionStatistics>)other.regions)
                .ForEach(o =>
                {
                    var copy = o;

                    // copy dictionary
                    copy.conquestsPerRealm = o.conquestsPerRealm?.ToDictionary(kv => kv.Key, kv => kv.Value);

                    return copy;
                })
                .ToArray();
        }
    }

}