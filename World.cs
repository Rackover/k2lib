
namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    public struct World : IBinarySerializableWithVersion
    {
        private const byte OPTIMAL_REALM_SIZE = 1;

        private struct AxialPosition
        {
            public int q;
            public int r;

            public AxialPosition(int q, int r)
            {
                this.q = q;
                this.r = r;
            }

            public AxialPosition(Position p)
            {
                q = p.x - (p.y - (p.y & 1)) / 2;
                r = p.y;
            }

            public Position ToPosition()
            {
                var x = q + (r - (r & 1)) / 2;
                var y = r;
                return new Position(x, y);
            }

            public static AxialPosition operator +(AxialPosition left, AxialPosition right)
            {
                return new AxialPosition(left.q + right.q, left.r + right.r);
            }

            public static AxialPosition operator *(AxialPosition left, int right)
            {
                return new AxialPosition(left.q * right, left.r * right);
            }
        }

        private static readonly IReadOnlyList<AxialPosition> AxialDirectionVectors = new AxialPosition[] {
            new AxialPosition(+1,  0),
            new AxialPosition( 0, +1),
            new AxialPosition(+1, -1),
            new AxialPosition(-1,  0),
            new AxialPosition( 0, -1),
            new AxialPosition(-1, +1),
        };

        public IReadOnlyList<Region> Regions => regions;
        public IReadOnlyList<Realm> Realms => realms;

        public byte SideLength => sideLength;

        public byte SquareSideLength => squareSideLength;


        private Region[] regions;

        private Realm[] realms;

        private Position[] startingPositions;

        private byte sideLength;

        private byte squareSideLength;

        private readonly GameRules rules;

        private byte? councilRealmIndex;

        public World(in PartySessionInitializationParameters playerParams, in GameRules parameters) : this()
        {
            this.rules = parameters;

            int realmCountWithoutCouncil = parameters.additionalRealmsCount + playerParams.realmsToInitialize.Length;
            int realmCount = realmCountWithoutCouncil + (parameters.hasCouncilRealm ? 1 : 0);

            sideLength = CalculateSideLength(
                parameters,
                realmCountWithoutCouncil, 
                out squareSideLength
            );

            regions = new Region[sideLength * sideLength];
            realms = new Realm[realmCount];

            EatColumnsRows();
            EatCorners();

            bool isRealmCountPowerOfTwo = (realmCountWithoutCouncil & (realmCountWithoutCouncil - 1)) == 0;
            Lib.Position[] positions = isRealmCountPowerOfTwo ?
                GetPossibleGridAlignedPositions() :
                GetPossibleRotationaryPositions(realmCountWithoutCouncil);

            if (positions.Length < realmCountWithoutCouncil) {
                throw new System.Exception($"Map is too small (needed to create {realms.Length} realms and have only {positions.Length}, square side is {squareSideLength})");
            }

            if (parameters.hasCouncilRealm) {
                Lib.Position middle = new Position(sideLength / 2, sideLength / 2);
                Lib.Position councilPosition = middle;
                int posIndex = -1;

                for (int i = 0; i < positions.Length; i++) {
                    if (positions[i] == middle) {
                        councilPosition = positions[i];
                        posIndex = i;
                    }
                }

                if (posIndex >= 0) {
                    positions[posIndex] = positions[^1];
                    positions[^1] = councilPosition;
                }
                else {
                    // Add a position just for council
                    Lib.Position[] newPositions = new Position[positions.Length + 1];
                    positions.CopyTo(newPositions, 0);
                    newPositions[positions.Length] = middle;
                    positions = newPositions;
                }
            }

            startingPositions = positions;

            for (byte i = 0; i < realms.Length; i++) {
                if (i == realms.Length - 1 && parameters.hasCouncilRealm) {
                    councilRealmIndex = i;
                    InitializeCouncilRealm(i, startingPositions[^1]);
                }
                else {
                    InitializeRealm(i, startingPositions[i], rules.initialRealmsSize);
                }
            }
        }

        public World(in World other) : this()
        {
            regions = new Region[other.regions.Length];
            other.regions.CopyTo(regions, 0);

            realms = new Realm[other.realms.Length];
            other.realms.CopyTo(realms, 0);

            startingPositions = other.startingPositions;
            rules = other.rules;

            sideLength = other.sideLength;
            squareSideLength = other.squareSideLength;
            councilRealmIndex = other.councilRealmIndex;
        }

        public static World Empty()
        {
            return new World(new GameRules());
        }

        private World(GameRules rules) : this()
        {
            this.rules = rules;
        }

        public void Modify(out Region[] regions, out Realm[] realms)
        {
            regions = this.regions;
            realms = this.realms;
        }

        public void AddSilverTreasury(byte realmIndex, int amount)
        {
            int treasury = GetSilverTreasury(realmIndex);
            SetSilverTreasury(realmIndex, treasury+ amount);
        }

        public void SetSilverTreasury(byte realmIndex, int treasury)
        {
            byte target = realmIndex;
            if (realms[realmIndex].IsSubjugated(out byte subjugator)) {
                target = subjugator;
            }

            if (treasury < realms[target].silverTreasury) {
                realms[target].totalSpentStatistics += realms[target].silverTreasury - treasury;
            }

            realms[target].silverTreasury = treasury;
        }

        public int GetSilverTreasury(byte realmIndex)
        {
            if (realms[realmIndex].IsSubjugated(out byte subjugator)) {
                return realms[subjugator].silverTreasury;
            }

            return realms[realmIndex].silverTreasury;
        }

        private static byte CalculateSideLength(GameRules rules, int realmCountWithoutCouncil, out byte squareSideLength)
        {
            // 1,2,3,4 => 2
            // 5,6,7,8,9 => 3

            // Small hack to bump if we have 9 realms on a 3v3 grid so that the council can have the central space
            if (realmCountWithoutCouncil == 9 && rules.hasCouncilRealm) {
                realmCountWithoutCouncil++;
            }

            squareSideLength = (byte)Math.Ceiling(Math.Sqrt(realmCountWithoutCouncil));

            squareSideLength = Math.Max((byte)2, squareSideLength);

            int sideLength = rules.initialSafetyMarginBetweenRealms + (1 + rules.initialSafetyMarginBetweenRealms + /*rules.initialRealmsSize*/ 1 * 2) * squareSideLength;

            return (byte)sideLength;
        }

        public bool IsCouncilRegion(int regionIndex) => councilRealmIndex.HasValue && this.regions[regionIndex].IsOwnedBy(councilRealmIndex.Value);

        public bool IsCouncilRealm(byte realmIndex) => councilRealmIndex == realmIndex;

        public bool IsRealmExcludedFromVoting(byte realmIndex) => IsCouncilRealm(realmIndex) || this.realms[realmIndex].isSubjugated;

        public EFactionFlag GetRealmFaction(int realmIndex)
        {
            return this.rules.factions.flagsForFaction[realms[realmIndex].factionIndex];
        }

        public bool GetRegionFactionIndex(int regionIndex, out byte factionIndex)
        {
            if (regions[regionIndex].GetOwner(out byte realmIndex)) {
                factionIndex = realms[realmIndex].factionIndex;
                return true;
            }

            factionIndex = default;
            return false;
        }

        public EFactionFlag GetRegionFaction(int regionIndex)
        {
            if (regions[regionIndex].GetOwner(out byte realmIndex)) {
                return GetRealmFaction(realmIndex);
            }

            return EFactionFlag.None;
        }

        public int GetRegionSilverWorth(int regionIndex)
        {
            EFactionFlag faction = GetRegionFaction(regionIndex);
            return regions[regionIndex].GetSilverWorth(faction, rules);
        }

        public int GetRegionLootableSilverWorth(int regionIndex, byte lootingRealm)
        {
            int silver;

            if (regions[regionIndex].buildings.HasFlagSafe(EBuilding.Capital) && !rules.subjugationEnabled) {
                silver = rules.silverLootedOnCapital;
            }
            else {
                silver =GetRegionSilverWorth(regionIndex);
            }

            if (GetRealmFaction(lootingRealm).HasFlagSafe(EFactionFlag.LootMoreMoney)) {
                if (silver < rules.factions.looterMinimumSilver) {

                    silver = rules.factions.looterMinimumSilver;
                    if (GetRegionFaction(regionIndex).HasFlagSafe(EFactionFlag.RicherTerritories)) {
                        silver *= rules.factions.looterRichesMultiplier;
                    }
                }
                else {

                }
            }
         
            return silver;
        }

        public bool CanRealmAttackRegion(byte realmIndex, int regionIndex)
        {
            if (regions[regionIndex].inert) {
                return false;
            }

            if (regions[regionIndex].IsOwnedBy(realmIndex)) {
                return false;
            }

            if (IsCouncilRegion(regionIndex)) {
                return false;
            }

            if (regions[regionIndex].GetOwner(out byte regionOwner)) {
                if (realms[regionOwner].IsSubjugated(out byte theirOwner)) {
                    regionOwner = theirOwner;
                }

                if (realms[realmIndex].IsSubjugated(out byte myOwner)) {
                    realmIndex = myOwner;
                }

                if (regionOwner == realmIndex) {
                    return false; // We're friends now
                }
            }

            return true;
        }

        public bool GetAttackTargetsForRegionNoAlloc(int regionIndex, bool canExtendRange, in List<int> attackTargets)
        {
            if (regions[regionIndex].inert)
                return false;

            int countBefore = attackTargets.Count;

            int range = 1;

            if (canExtendRange) {
                EFactionFlag attackingFaction = GetRegionFaction(regionIndex);
                if (attackingFaction.HasFlagSafe(EFactionFlag.Charge)) {
                    range = 2;
                }
            }

            Position position = Position(regionIndex);
            AxialPosition axialCenter = new AxialPosition(position);

            for (int i = 0; i < AxialDirectionVectors.Count; i++) {
                AxialPosition direction = AxialDirectionVectors[i];
                for (int distance = 1; distance <= range; distance++) {
                    Position neighborPosition = (axialCenter + direction * distance).ToPosition();

                    if (!IsValidPosition(neighborPosition)) {
                        break;
                    }

                    int neighborIndex = Index(neighborPosition);

                    if (!IsValidIndex(neighborIndex)) {
                        break;
                    }

                    if (!CanRealmAttackRegion(regions[regionIndex].ownerIndex, neighborIndex)) {
                        break;
                    }

                    attackTargets.Add(neighborIndex);
                }
            }

            return attackTargets.Count > countBefore;
        }

        public bool GetAttackTargetsForRegion(int regionIndex, bool canExtendAttack, out List<int> attackTargets)
        {
            attackTargets = new();

            return GetAttackTargetsForRegionNoAlloc(regionIndex, canExtendAttack, in attackTargets);
        }

        public bool GetNaturalOwnerFromNeighbors(int regionIndex, ManagedRandom randomOptional, out byte newOwner, out bool wasCoinFlip, out bool isTotallySurrounded)
        {
            int[] neighbors = GetNeighboringRegions(regionIndex);

            int maxOwnedConnections = 0;
            int ownedNeighbors = 0;

            Dictionary<byte, int> neighboringConnections = new Dictionary<byte, int>(neighbors.Length);

            for (int i = 0; i < neighbors.Length; i++) {
                if (regions[neighbors[i]].isOwned) {
                    byte owner = regions[neighbors[i]].ownerIndex;

                    if (IsCouncilRealm(owner)) {
                        continue;
                    }

                    if (neighboringConnections.ContainsKey(owner)) {
                        neighboringConnections[owner]++;
                    }
                    else {
                        neighboringConnections.Add(owner, 1);
                    }

                    maxOwnedConnections = Math.Max(maxOwnedConnections, neighboringConnections[owner]);
                    ownedNeighbors++;
                }
            }

            List<byte> potentialOwners = new List<byte>();
            foreach (var kv in neighboringConnections) {
                if (kv.Value >= maxOwnedConnections) {

                    if (IsCouncilRealm(kv.Key)) { // should not be necessary and yet ??
                        continue;
                    }

                    potentialOwners.Add(kv.Key);
                }
            }

            newOwner = default;
            wasCoinFlip = false;
            if (potentialOwners.Count > 0) {
                if (potentialOwners.Count > 1) {
                    // Null means random is not allowed for resolution here
                    if (randomOptional == null) {
                        potentialOwners.Clear();
                    }
                    else {
                        newOwner = potentialOwners[randomOptional.Next(potentialOwners.Count)];
                        wasCoinFlip = true;
                    }
                }
                else {
                    newOwner = potentialOwners[0];
                }
            }

            isTotallySurrounded = potentialOwners.Count == 1 && ownedNeighbors == maxOwnedConnections;

            return potentialOwners.Count > 0;
        }

        public void GetTerritoryOfRealm(byte realmIndex, in List<int> regions, Predicate<Region> filter, bool includeSubjugated=false)
        {
            GetTerritoryOfRealm(realmIndex, regions);

            for (int i = 0; i < regions.Count; i++) {
                if (!filter(this.regions[regions[i]])) {
                    regions.RemoveAt(i);
                    i--;
                }
            }
        }

        private static readonly List<byte> realmPoolBuffer = new List<byte>();


        public void GetTerritoryOfRealm(byte realmIndex, in List<int> regions)
        {
            GetTerritoryOfRealm(realmIndex, regions, includeSubjugated: false);
        }

        public void GetTerritoryOfRealm(byte realmIndex, in List<int> regions, bool includeSubjugated)
        {
            if (rules.subjugationEnabled && includeSubjugated) {
                realmPoolBuffer.Clear();
                GetAlliedOrSubjugatedRealms(realmIndex, realmPoolBuffer);

                // Add up territories
                for (int i = 0; i < realmPoolBuffer.Count; i++) {
                    GetTerritoryOfRealm(realmPoolBuffer[i], regions, includeSubjugated: false);
                }
            }
            else {
                if (GetCapitalOfRealm(realmIndex, out int capital)) {
                    GetAllConnectedRegionsOfSameOwner(capital, regions);
                }
            }
        }

        public void GetAlliedOrSubjugatedRealms(byte realmIndex, in List<byte> realmPool)
        {
            realmPool.Add(realmIndex);

            if (realms[realmIndex].IsSubjugated(out byte myRuler)) {
                realmPool.Add(myRuler);
            }

            for (byte i = 0; i < realms.Length; i++) {
                if (i == realmIndex) {
                    continue;
                }

                if (realms[i].IsSubjugated(out byte subjugator) && (subjugator == realmIndex || subjugator == myRuler)) {
                    if (!realmPool.Contains(i)) {
                        realmPool.Add(i);
                    }
                }
            }
        }

        public bool GetCapitalOfRealm(byte realmIndex, out int regionIndex)
        {
            for (regionIndex = 0; regionIndex < regions.Length; regionIndex++) {
                if (regions[regionIndex].buildings.HasFlagSafe(EBuilding.Capital) && regions[regionIndex].IsOwnedBy(realmIndex)) {
                    return true;
                }
            }

            return false;
        }

        public void GetAllConnectedRegions(int startingPoint, in List<int> regionsIndices, Predicate<Region> filter)
        {
            regionsIndices.Add(startingPoint);
            int[] neighbors = GetNeighboringRegions(startingPoint);
            byte owner = regions[startingPoint].ownerIndex;

            bool hasOwner = regions[startingPoint].isOwned;

            for (int i = 0; i < neighbors.Length; i++) {
                if (regionsIndices.Contains(neighbors[i])) {
                    continue;
                }

                if (!filter(regions[neighbors[i]])) {
                    continue;
                }

                GetAllConnectedRegions(neighbors[i], in regionsIndices, filter);
            }
        }

        public void GetAllConnectedRegionsOfSameOwner(int startingPoint, in List<int> regionsIndices)
        {
            bool hasOwner = regions[startingPoint].isOwned;
            byte owner = regions[startingPoint].ownerIndex;

            GetAllConnectedRegions(startingPoint, regionsIndices, (r) => 
                (!r.isOwned && !hasOwner) ||
                (hasOwner && r.IsOwnedBy(owner))
            );
        }

        public Position Position(int index)
        {
            return new Position(index % SideLength, index / SideLength);
        }

        public bool GetOppositeNeighbor(int centralRegionIndex, int neighborRegionIndex, out int oppositeRegionIndex)
        {
            Position centralPosition = Position(centralRegionIndex);
            Position neighborPosition = Position(neighborRegionIndex);
            Position opposite = GetOppositePosition(centralPosition, neighborPosition);

            oppositeRegionIndex = Index(opposite);

            return IsValidIndex(oppositeRegionIndex);
        }

        private Position GetOppositePosition(in Position centralPoint, in Position positionToMirror)
        {
            AxialPosition axialCentral = new AxialPosition(centralPoint);
            AxialPosition axialNeighbor = new AxialPosition(positionToMirror);


            AxialPosition axialDir = new AxialPosition() {
                q = axialNeighbor.q - axialCentral.q,
                r = axialNeighbor.r - axialCentral.r
            };

            AxialPosition axialOpposite = new AxialPosition() {
                q = axialCentral.q - axialDir.q,
                r = axialCentral.r - axialDir.r
            };

            Position outputPosition = axialOpposite.ToPosition();

            return outputPosition;
        }

        public void GetNeighboringRegions(int index, in List<int> neighbors)
        {
            Position position = Position(index);

            int offset = 1 - position.y % 2;

            if (position.x > 0) {
                neighbors.Add(index - 1);
            }

            if (position.y > 0) {
                if (position.x >= offset) {
                    neighbors.Add(index - SideLength - offset);
                }

                if (position.x < SideLength - 1 + offset) {
                    neighbors.Add(index - SideLength + 1 - offset);
                }
            }

            if (position.x < SideLength - 1) {
                neighbors.Add(index + 1);
            }

            if (position.y < SideLength - 1) {

                if (position.x < SideLength - 1 + offset) {
                    neighbors.Add(index + SideLength + 1 - offset);
                }

                if (position.x >= offset) {
                    neighbors.Add(index + SideLength - offset);
                }
            }

            int maxIndex = SideLength * SideLength - 1;
            neighbors.RemoveAll(o => o < 0 || o > maxIndex);
            for (int i = 0; i < neighbors.Count; i++) {
                if (regions[neighbors[i]].inert) {
                    neighbors.RemoveAt(i);
                    i--;
                }
            }
        }

        private void EatColumnsRows()
        {
            int eatenColumns = Math.Max(0, rules.eatFirstLastColumns - 3 + squareSideLength);

            for (int x = 0; x < SideLength; x++) {
                if (x < eatenColumns || x >= SideLength- eatenColumns) {
                    for (int y = 0; y < SideLength; y++) {
                        Position position = new Position(x, y);
                        int index = Index(position);
                        regions[index].inert = true;
                    }
                }
            }
        }

        private void EatCorners()
        {
            int eatenCorners = Math.Max(0, rules.eatenCorners - 2 + squareSideLength);

            for (int x = 0; x < SideLength; x++) {
                for (int y = 0; y < SideLength; y++) {

                    int invX = SideLength - x - 1;
                    int invY = SideLength - y - 1;

                    void eat(ref World world, int rX, int rY, int eatenCorners)
                    {
                        if (rX + rY <= eatenCorners) {
                            Position position = new Position(x, y);
                            int index = world.Index(position);

                            world.regions[index].inert = true;
                        }
                    }

                    eat(ref this, x, y, System.Math.Max(0, eatenCorners - 1));
                    eat(ref this, invX, y, eatenCorners);
                    eat(ref this, invX, invY, eatenCorners);
                    eat(ref this, x, invY, System.Math.Max(0, eatenCorners - 1));
                }
            }
        }

        private Position[] GetPossibleRotationaryPositions(int positionCount)
        {
            List<Position> positions = new List<Position>();

            int anglePerStep = 360 / positionCount;

            int distanceFromCenter = (SquareSideLength/3) * OPTIMAL_REALM_SIZE
                + OPTIMAL_REALM_SIZE + 1 + rules.initialSafetyMarginBetweenRealms;

            Position centerPosition = new Position(sideLength / 2, sideLength / 2);

            for (int i = 0; i < positionCount; i++) {

                int angle = i * anglePerStep;

                int sinePercentage = TrigonometryHelper.GetSineM100100(angle);
                int cosinePercentage = TrigonometryHelper.GetCosineM100100(angle);

                Position offset = new Position(sinePercentage * distanceFromCenter + 50, cosinePercentage * distanceFromCenter);

                Position position = (centerPosition * 100 + offset) / 100;

                positions.Add(position);
            }

            return SortPositionsByFurtherFromEachOther(positions);
        }

        private Position[] GetPossibleGridAlignedPositions()
        {
            List<Position> positions = new List<Position>();

            int margin = OPTIMAL_REALM_SIZE + rules.initialSafetyMarginBetweenRealms;
            int realmSquareSize = OPTIMAL_REALM_SIZE * 2 + 1;

            int realmsPerRow = squareSideLength;

            // Realms can't be glued to each other and have to be not touching the outer or inner ring
            for (int x = 0; x < realmsPerRow; x++) {
                for (int y = 0; y < realmsPerRow; y++) {

                    int posX = margin;
                    int posY = margin;

                    int spacePerRealm = realmSquareSize + rules.initialSafetyMarginBetweenRealms;

                    posX += x * spacePerRealm;

                    posY += y * spacePerRealm;

                    positions.Add(new Lib.Position(posX, posY));
                }
            }

            return SortPositionsByFurtherFromEachOther(positions);
        }

        private Position[] SortPositionsByFurtherFromEachOther(List<Position> positions)
        {
            // Group distances that are the furthest from each other
            List<Position> sortedPositions = new List<Position>(positions.Count);
            while (positions.Count > 0) {
                if (sortedPositions.Count == 0) {
                    sortedPositions.Add(positions[0]);
                    positions.RemoveAt(0);
                }
                else {
                    Position gravityCenter = new Position();
                    for (int existingPositionIndex = 0; existingPositionIndex < sortedPositions.Count; existingPositionIndex++) {
                        gravityCenter += sortedPositions[existingPositionIndex];
                    }

                    gravityCenter /= sortedPositions.Count;

                    positions.Sort((a, b) => b.SquaredDistanceWith(gravityCenter).CompareTo(a.SquaredDistanceWith(gravityCenter)));
                    sortedPositions.Add(positions[0]);
                    positions.RemoveAt(0);
                }
            }

            return sortedPositions.ToArray();
        }

        private void InitializeCouncilRealm(byte realmIndex, in Position startingPosition)
        {
            InitializeRealm(realmIndex, startingPosition, (byte)Math.Max(0, SquareSideLength - 3 + rules.councilRealmRegionSize));
            for (int i = 0; i < regions.Length; i++) {
                if (IsCouncilRegion(i) && regions[i].buildings == EBuilding.None) {
                    regions[i].buildings = EBuilding.Church;
                }
            }
        }

        private void InitializeRealm(byte realmIndex, in Position startingPosition, byte size=1)
        {
            ref Region region = ref regions[Index(startingPosition)];

            region.buildings = EBuilding.Capital;
            region.ownerIndex = realmIndex;
            region.isOwned = true;

            // Expand until top size reached
            int remainingExpansion = size;
            List<int> cache = new List<int>();
            while (remainingExpansion > 0) {
                remainingExpansion--;
                cache.Clear();
                GetTerritoryOfRealm(realmIndex, cache);
                for (int i = 0; i < cache.Count; i++) {
                    int[] neighbors = GetNeighboringRegions(cache[i]);
                    for (int neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++) {
                        ref Region ownedRegion = ref regions[neighbors[neighborIndex]];
                        if (!ownedRegion.IsOwnedBy(realmIndex)) {
                            if (!ownedRegion.isOwned || i % 2 == 0) { // "Flip flop" if the region overlaps with another starting area
                                ownedRegion.ownerIndex = realmIndex;
                                ownedRegion.isOwned = true;
                            }
                        }
                    }
                }
            }

            ref Realm realm = ref realms[realmIndex];
            realm.silverTreasury = this.rules.startingGold * 10;
            realm.availableDecisions = this.rules.startingDecisionCount;
        }

        private int[] GetNeighboringRegions(int index)
        {
            List<int> neighbors = new List<int>(6);

            GetNeighboringRegions(index, neighbors);

            return neighbors.ToArray();
        }

        private int Index(in Position position)
        {
            return position.x + position.y * SideLength;
        }

        private bool IsValidPosition(in Position position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < SideLength && position.y < SideLength;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < regions.Length;
        }

        public void Write(BinaryWriter into)
        {
            into.Write(sideLength);
            into.Write(squareSideLength);

            into.Write(councilRealmIndex.HasValue);
            into.Write(councilRealmIndex ?? (byte)0);
            into.Write(regions);
            into.Write(realms);
            into.Write(startingPositions);
            into.Write(rules);
        }

        public void Read(byte version, BinaryReader from)
        {
            sideLength = from.ReadByte();
            squareSideLength = from.ReadByte();

            councilRealmIndex = null;
            bool hasCouncilRealmIndex = from.ReadBoolean();
            byte realmIndex = from.ReadByte();
            if (hasCouncilRealmIndex) {
                councilRealmIndex = realmIndex;
            }

            from.Read(default, ref regions);
            from.Read(default, ref realms);
            from.Read(default, ref startingPositions);

            rules.Read(default, from);
        }

        public int GetHash()
        {
            return Extensions.Hash(
                Extensions.Hash(
                    councilRealmIndex ?? 0
                ),
                Extensions.Hash(
                    Extensions.Hash(regions),
                    Extensions.Hash(realms),
                    Extensions.Hash(startingPositions),
                    Extensions.Hash(rules)
                )
            );
        }
    }
}