
namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public struct GameState : IBinarySerializableWithVersion
    {
        public int daysPassed;
        public int councilsPassed;
        public int daysRemainingBeforeNextCouncil;
        public World world;
        public Voting voting;
        public GameRules rules;

        public GameState(PartySessionInitializationParameters party, GameRules parameters)
        {
            rules = parameters;
            councilsPassed = 0;
            daysPassed = 0;
            daysRemainingBeforeNextCouncil = parameters.turnsBetweenVotes;
            world = new World(party, parameters);
            voting = new Voting(parameters);
        }

        public GameState(in GameState other)
        {
            rules = other.rules;
            councilsPassed = other.councilsPassed;
            daysPassed = other.daysPassed;
            daysRemainingBeforeNextCouncil = other.daysRemainingBeforeNextCouncil;
            world = new World(other.world);
            voting = new Voting(other.voting);
        }

        public void ComputeEffects(ManagedRandom random, IReadOnlyList<Transform> transformsUnordered, out ITransformEffect[] effects)
        {
            Logger.Trace($"Computing {transformsUnordered.Count} UNORDERED transforms on {this}");
            List<string> debugStrings = transformsUnordered.Select(o => o.ToString()).OrderBy((o)=>o).ToList();

            for (int i = 0; i < debugStrings.Count; i++) {
                Logger.Trace($"{i}: {debugStrings[i]}");
            }

            List<ITransformEffect> effectsList = new List<ITransformEffect>();

            List<Transform> remainingTransforms = transformsUnordered.ToList();

            // Priority buildings
            {
                var constructions = TakeConstructions(world, remainingTransforms, priorityOnly: true);
                PlayConstructions(in world, constructions, effectsList);
            }

            // Attacks
            {
                GameState attacksComputationDuplicate = Duplicate();
                ApplyEffects(effectsList, ref attacksComputationDuplicate);

                int attacksStart = effectsList.Count;

                var attacks = TakeAttacks(attacksComputationDuplicate.world, remainingTransforms);
                PlayAttacks(in attacksComputationDuplicate.world, random, attacks, effectsList);

                // Order subjugations AFTER attacks
                List<ITransformEffect.ConquestEffect> conquestsThatLeadToSubjugations = new List<ITransformEffect.ConquestEffect>();
                HashSet<int> queuedEffectGroup = new HashSet<int>();
                HashSet<byte> subjugationTargets = new HashSet<byte>();

                for (int i = attacksStart; i < effectsList.Count; i++) {

                    if (i < effectsList.Count-1) {
                        if (effectsList[i] is ITransformEffect.ConquestEffect conq && 
                            effectsList[i+1] is ITransformEffect.SubjugationEffect subjugation &&
                            subjugation.attackingRealmIndex ==conq.attackingRealm) {

                            queuedEffectGroup.Add(i);

                            subjugationTargets.Add(subjugation.targetRealmIndex);
                        }
                    }
                }

                for (int i = effectsList.Count-1; i >= 0; i--) {
                    if (queuedEffectGroup.Contains(i)) {
                        effectsList.Add(effectsList[i]);
                        effectsList.Add(effectsList[i + 1]);
                        effectsList.RemoveAt(i+1);
                        effectsList.RemoveAt(i);
                    }
                }

                // Cannot subjugate if you're the target of a subjugation
                effectsList.RemoveAll(o => 
                    o is ITransformEffect.SubjugationEffect sub &&
                    subjugationTargets.Contains(sub.attackingRealmIndex)
                );
            }

            // Border gore
            {
                GameState borderGoreComputationDuplicate = Duplicate();
                
                // From realms
                byte depth = 0;
                while (true) {
                    int count = effectsList.Count;
                    ApplyEffects(effectsList, ref borderGoreComputationDuplicate);
                    borderGoreComputationDuplicate.ResolveRealmBorderGore(in borderGoreComputationDuplicate.world, random, effectsList, depth);

                    depth++;

                    if (effectsList.Count > count) {
                        continue;
                    }

                    break;
                }

                // From Neutral
                if (rules.neutralRegionStarvation) {
                    depth = 0;
                    while (true) {
                        int count = effectsList.Count;
                        ApplyEffects(effectsList, ref borderGoreComputationDuplicate);
                        borderGoreComputationDuplicate.ResolveNeutralBorderGore(in borderGoreComputationDuplicate.world, effectsList, depth);

                        depth++;

                        if (effectsList.Count > count) {
                            continue;
                        }

                        break;
                    }
                }
            }

            // Construction
            {
                var constructions = TakeConstructions(world, remainingTransforms);

                PlayConstructions(in world, constructions, effectsList);
            }

            // Others - We play them first to avoid visual oddities
            {
                foreach (var t in remainingTransforms) {
                    if (t is AdminUpgradeTransform adminUpgrade) {
                        effectsList.Insert(0, new ITransformEffect.AdministrationUpgradeEffect() {
                            realmIndex = adminUpgrade.realmToUpgrade,
                            silverPricePaid = adminUpgrade.silverPricePaid
                        });
                    }
                    else if (t is PayFavoursTransform payFavoursTransform) {
                        effectsList.Insert(0, new ITransformEffect.FavourPaymentEffect() {
                            realmIndex = payFavoursTransform.realmToFavour,
                            silverPricePaid = payFavoursTransform.silverPricePaid
                        });
                    }
                }
            }

            effects = effectsList.ToArray();
        }

        public void ApplyEffects(IReadOnlyList<ITransformEffect> effects, ref GameState newState)
        {
            Logger.Trace($"Applying {effects.Count} effects {this}");

            for (int i = 0; i < effects.Count; i++) {
                Logger.Trace($"{i}: {effects[i]}");

                effects[i].Apply(this, ref newState);
            }
        }

        public int GetHash()
        {
            return Extensions.Hash(
                Extensions.Hash(
                    daysPassed,
                    councilsPassed
                ),
                Extensions.Hash(world),
                Extensions.Hash(voting)
            );
        }

        public GameState Duplicate()
        {
            GameState gameState = this;
            gameState.world = new World(world);

            return gameState;
        }

        private void ResolveNeutralBorderGore(in World world, in List<ITransformEffect> effects, byte depth)
        {
            // Next we solve border gore for neutral regions
            {
                List<int> isolatedNeutralRegions = new List<int>();
                List<int> unOwnedRegions = new List<int>(Enumerable.Range(0, world.Regions.Count));
                for (int i = 0; i < unOwnedRegions.Count; i++) {
                    int regionIndex = unOwnedRegions[i];
                    if (world.Regions[regionIndex].GetOwner(out _) || world.Regions[regionIndex].inert) {
                        unOwnedRegions.RemoveAt(i);
                        i--;
                        continue;
                    }
                }

                List<int> connected = new List<int>();
                List<int> neighborsBuffer = new List<int>();
                while (unOwnedRegions.Count > 0) {
                    connected.Clear();
                    int first = unOwnedRegions[0];
                    world.GetAllConnectedRegionsOfSameOwner(first, connected);
                    unOwnedRegions.RemoveAll(connected.Contains);

                    bool isolated = true;

                    byte? ownerNeighborFound = null;

                    for (int i = 0; i < connected.Count; i++) {
                        int regionIndex = connected[i];
                        neighborsBuffer.Clear();
                        world.GetNeighboringRegions(regionIndex, neighborsBuffer);

                        if (neighborsBuffer.Count < 6) {
                            // This means it's touching a terrain border
                            isolated = false;
                        }
                        else {
                            for (int neighborIndexIndex = 0; neighborIndexIndex < neighborsBuffer.Count; neighborIndexIndex++) {
                                int neighborRegionIndex = neighborsBuffer[neighborIndexIndex];

                                if (world.IsCouncilRegion(neighborRegionIndex)) {
                                    isolated = false;
                                    break;
                                }
                                else if (world.Regions[neighborRegionIndex].inert) {
                                    isolated = false;
                                    break;
                                }
                                else if (rules.goTakeNeutralOnlyWhenNoContest && world.Regions[neighborRegionIndex].isOwned) {
                                        // Go-take of unowned regions can only be accomplished when the takeover is TOTAL
                                        // that means no contested borders of the target region group
                                    if (ownerNeighborFound.HasValue) {
                                        if (world.Regions[neighborRegionIndex].ownerIndex != ownerNeighborFound.Value) {
                                            isolated = false;
                                            break;
                                        }
                                    }
                                    else {
                                        ownerNeighborFound = world.Regions[neighborRegionIndex].ownerIndex;
                                    }
                                }
                            }
                        }

                        // If this one is not isolated, then none of the others in the chain are
                        if (isolated == false) {
                            break;
                        }
                    }

                    if (isolated) {
                        // If this is true, then ALL of those regions are to be solved!!
                        // Somebody's playing Go
                        isolatedNeutralRegions.AddRange(connected);
                    }
                }

                for (int i = 0; i < isolatedNeutralRegions.Count; i++) {
                    int regionIndex = isolatedNeutralRegions[i];
                    // Random is NOT ALLOWED!
                    SolveBorderGoreForRegion(world, randomOptional: null, regionIndex, effects, depth);
                }
            }
        }

        private void ResolveRealmBorderGore(in World world, ManagedRandom random, in List<ITransformEffect> effects, byte depth)
        {
            List<ITransformEffect> effectsToAdd = new List<ITransformEffect>();
            Dictionary<byte, List<int>> allRegionsForRealm = new Dictionary<byte, List<int>>(world.Realms.Count);
            List<int> totalOwnedRegions = new List<int>();

            for (int regionIndex = 0; regionIndex < world.Regions.Count; regionIndex++) {
                if (world.Regions[regionIndex].isOwned) {
                    byte owner = world.Regions[regionIndex].ownerIndex;

                    if (!allRegionsForRealm.TryGetValue(owner, out List<int> allRegions)) {
                        allRegions = new List<int>();
                        allRegionsForRealm[owner] = allRegions;
                    }

                    allRegionsForRealm[owner].Add(regionIndex);
                    totalOwnedRegions.Add(regionIndex);
                }
            }

            List<int> connectedRealmRegions = new List<int>();

            List<byte> alliedRealmsCache = new List<byte>();

            for (byte startingRealmIndex = 0; startingRealmIndex < world.Realms.Count; startingRealmIndex++) {
                connectedRealmRegions.Clear();
                Realm realm = world.Realms[startingRealmIndex];

                alliedRealmsCache.Clear();
                world.GetAlliedOrSubjugatedRealms(startingRealmIndex, alliedRealmsCache);

                List<int> remainingTerritories = new List<int>(allRegionsForRealm[startingRealmIndex]);

                for (int realmIndexIndex = 0; realmIndexIndex < alliedRealmsCache.Count; realmIndexIndex++) {
                    byte realmIndex = alliedRealmsCache[realmIndexIndex];

                    bool isAlliedRegion(Region region)
                    {
                        return region.isOwned && alliedRealmsCache.Contains(region.ownerIndex);
                    }

                    if (world.GetCapitalOfRealm(realmIndex, out int capitalRegionIndex)) {
                        world.GetAllConnectedRegions(capitalRegionIndex, connectedRealmRegions, isAlliedRegion);

                        var faction = world.GetRealmFaction(realmIndex);

                        remainingTerritories.RemoveAll(connectedRealmRegions.Contains);

                        if (faction.HasFlagSafe(EFactionFlag.FortsCountAsCapital)) {

                            // Add regions that are connected to a fort
                            for (int territoryIndexIndex = 0; territoryIndexIndex < remainingTerritories.Count; territoryIndexIndex++) {
                                int regionIndex = remainingTerritories[territoryIndexIndex];

                                // Mark these as connected if they have at least one fort
                                if (world.Regions[regionIndex].buildings.HasFlagSafe(EBuilding.Fort)) {
                                    world.GetAllConnectedRegions(regionIndex, connectedRealmRegions, isAlliedRegion);
                                    remainingTerritories.RemoveAll(connectedRealmRegions.Contains);
                                    territoryIndexIndex = -1; // Set to minus one to restart loop. Then continue
                                    continue;
                                }
                            }
                        }

                        if (remainingTerritories.Count > 0) {
                            // That realm has border gore that we must solve
                            for (int i = 0; i < remainingTerritories.Count; i++) {
                                int regionIndex = remainingTerritories[i];
                                SolveBorderGoreForRegion(world, random, regionIndex, effectsToAdd, depth);

                                totalOwnedRegions.Add(regionIndex);
                            }
                        }
                        //else if (connectedRealmRegions.Count > allRegionsForRealm[realmIndex].Count) {
                        //    throw new System.Exception($"Something went wrong with border gore calculation");
                        //}
                    }
                }
            }

            // Coin flips at the very end
            effects.AddRange(effectsToAdd.OrderBy(o => o is ITransformEffect.ConquestEffect conquest && conquest.isACoinFlip));
        }

        private void SolveBorderGoreForRegion(in World world, ManagedRandom randomOptional, int regionIndex, in List<ITransformEffect> effects, byte depth)
        {
            if (world.GetNaturalOwnerFromNeighbors(regionIndex, randomOptional, out byte newOwner, out bool wasCoinFlip, out bool isTotallySurrounded)) {
                if (world.Regions[regionIndex].IsOwnedBy(newOwner)) {
                    // This happens - it's okay, the next pass of solving will fix it
                }
                else { 
                    effects.Add(new ITransformEffect.StarvationEffect() {
                        hasNewOwner = true,
                        newOwningRealm = newOwner,
                        regionIndex = regionIndex,
                        waveIndex = depth,
                        wasCoinFlip = wasCoinFlip
                    });
                }
            }
            else {
                // Lose ownership
                if (world.Regions[regionIndex].isOwned) {
                    effects.Add(new ITransformEffect.StarvationEffect() {
                        hasNewOwner = false,
                        newOwningRealm = default,
                        regionIndex = regionIndex,
                        waveIndex = depth,
                        wasCoinFlip = false
                    });
                }
            }
        }

        private void PlayAttackedRegion(in World world, ManagedRandom random, List<RegionAttackRegionTransform> attackOrders, in List<ITransformEffect> effects)
        {
            Logger.Trace($"Playing {attackOrders.Count} attacks on region {attackOrders[0].targetRegionIndex} : \n{string.Join('\n', attackOrders)}");

            bool majorityAttackingRealmIsACoinFlip = false;
            List<byte> otherCoinFlippers = new List<byte>();
            byte majorityAttackingRealm = 0;
            if (attackOrders.Count == 1) {
                majorityAttackingRealm = world.Regions[attackOrders[0].AttackingRegionIndex].ownerIndex;
            }
            else {
                Dictionary<byte, byte> attacksPerRealm = new(attackOrders.Count);
                Dictionary<int, byte> regionOwner = new(attackOrders.Count);
                List<byte> potentialAttackers = new List<byte>(attacksPerRealm.Count);

                byte biggestAmountOfAttacks = 0;

                for (int i = 0; i < attackOrders.Count; i++) {
                    byte attackingRealm = world.Regions[attackOrders[i].AttackingRegionIndex].ownerIndex;
                    if (!attacksPerRealm.ContainsKey(attackingRealm)) {
                        attacksPerRealm[attackingRealm] = 0;
                        potentialAttackers.Add(attackingRealm);
                    }

                    regionOwner[attackOrders[i].AttackingRegionIndex] = attackingRealm;

                    attacksPerRealm[attackingRealm]++;

                    biggestAmountOfAttacks = System.Math.Max(biggestAmountOfAttacks, attacksPerRealm[attackingRealm]);
                }

                // These will never win
                potentialAttackers.RemoveAll((o) => attacksPerRealm[o] < biggestAmountOfAttacks);

                // random choice
                if (potentialAttackers.Count > 1) {
                    majorityAttackingRealm = potentialAttackers[random.Next(potentialAttackers.Count)];
                    majorityAttackingRealmIsACoinFlip = true;
                    otherCoinFlippers.AddRange(potentialAttackers);
                    otherCoinFlippers.Remove(majorityAttackingRealm);
                }
                else {
                    majorityAttackingRealm = potentialAttackers[0];
                }

                attacksPerRealm[majorityAttackingRealm] = byte.MaxValue;

                // Put majority attacker at the end

                var sortedOrders = attackOrders
                    .OrderBy(o => attacksPerRealm[regionOwner[o.AttackingRegionIndex]])
                    .OrderBy(o => regionOwner[o.AttackingRegionIndex])
                    .ThenBy(o => o.isExtendedAttack)
                    .ToArray(); // Extended attacks at the end

                attackOrders.Clear();
                attackOrders.AddRange(sortedOrders);
            }

            // Play attack
            while (attackOrders.Count > 0) {
                List<int> attackingRegions = new List<int>();
                RegionAttackRegionTransform transform = attackOrders[0];
                byte attackOwner = world.Regions[transform.AttackingRegionIndex].ownerIndex;

                for (int i = 0; i < attackOrders.Count; i++) {
                    byte otherAttackOwner = world.Regions[attackOrders[i].AttackingRegionIndex].ownerIndex;
                    if (
                        // This is one of my attacks
                        otherAttackOwner == attackOwner ||

                        // These are part of a coin flip and must be played together
                        majorityAttackingRealmIsACoinFlip && 
                            (otherCoinFlippers.Contains(otherAttackOwner) || otherAttackOwner == majorityAttackingRealm)
                    ) {
                        attackingRegions.Add(attackOrders[i].AttackingRegionIndex);

                        attackOrders.RemoveAt(i);
                        i--;
                    }
                    else {
                        break;
                    }
                }

                if (majorityAttackingRealmIsACoinFlip) {
                    attackOwner = majorityAttackingRealm; // For faction feats & others, they're the attack owner now
                }

                Region target = world.Regions[transform.targetRegionIndex];
                EFactionFlag attackingFaction = world.GetRealmFaction(attackOwner);

                ITransformEffect.ConquestEffect effect = new ITransformEffect.ConquestEffect();
                effect.regionIndex = transform.targetRegionIndex;
                effect.attackingRealm = attackOwner;
                effect.attackingRegionsIndices = attackingRegions.ToArray();
                effect.previousOwningRealm = target.isOwned ? target.ownerIndex : byte.MaxValue;
                effect.newOwningRealm = effect.previousOwningRealm;

                effect.hadBuilding = target.buildings != EBuilding.None;
                effect.isACoinFlip = majorityAttackingRealmIsACoinFlip;
                effect.otherMajorityAttackersWhoLostCoinFlip = otherCoinFlippers.ToArray();

                // Extended attack is a prowess
                if (transform.isExtendedAttack) {
                    effect.factionHighlights |= EFactionFlag.Charge;
                }

                if (target.CannotBeTaken(rules, attackingFaction)) {
                    // It's a fail
                }
                else if (transform.isExtendedAttack &&
                    effects.Find((o) =>
                        o is ITransformEffect.ConquestEffect conquest &&
                        conquest.Success &&
                        conquest.attackingRegionsIndices.Contains(transform.AttackingRegionIndex)
                    ) == null) {
                    // Failure - extended attack that came after a tradtional attack that failed
                }
                else {
                    if (target.IsReinforcedAgainstAttack(rules, attackingFaction)) {

                        int attacks = attackingRegions.Count();

                        if (attacks > 1) {
                            effect.newOwningRealm = attackOwner;
                        }
                    }
                    else {
                        effect.newOwningRealm = attackOwner;
                    }
                }

                bool canLoot = !effect.Success && target.buildings != EBuilding.None;
                if (target.buildings.HasFlagSafe(EBuilding.Fort)
                    && effect.Success
                    && attackingFaction.HasFlagSafe(EFactionFlag.ConqueredFortsGivePayout)) {

                    effect.silverLooted = this.rules.factions.conqueredFortPayout;
                    canLoot = true;
                    effect.factionHighlights |= EFactionFlag.ConqueredFortsGivePayout;
                }
                else {
                    effect.silverLooted = canLoot ?
                        world.GetRegionLootableSilverWorth(transform.targetRegionIndex, attackOwner) * attackingRegions.Count :
                        0;
                }

                // Building capture is a prowess
                if (effect.Success &&
                    target.buildings != EBuilding.None &&
                   attackingFaction.HasFlagSafe(EFactionFlag.ConquestBuilding) &&
                    target.buildings != EBuilding.Capital
                    ) {
                    effect.factionHighlights |= EFactionFlag.ConquestBuilding;
                }

                // Epic loot from a mighty quest
                if (effect.silverLooted > 0 && attackingFaction.HasFlagSafe(EFactionFlag.LootMoreMoney)) {
                    effect.factionHighlights |= EFactionFlag.LootMoreMoney;
                }

                effects.Add(effect);

                // Subjugation
                bool canSubjugate = rules.subjugationForAll
                    || attackingFaction.HasFlagSafe(EFactionFlag.Subjugate) 
                    || world.Realms[attackOwner].IsSubjugated(out byte subjugator) && world.GetRealmFaction(subjugator).HasFlagSafe(EFactionFlag.Subjugate);

                if (canSubjugate && 
                    target.isOwned &&
                    effect.Success && 
                    target.buildings == EBuilding.Capital) {

                    var subjugation = new ITransformEffect.SubjugationEffect() {
                        attackingRealmIndex = attackOwner,
                        targetRealmIndex = effect.previousOwningRealm,
                        isFactionHighlight = !rules.subjugationForAll
                    };

                    effects.Add(subjugation);
                }
            }
        }

        private void PlayAttacks(in World world, ManagedRandom random, List<RegionAttackRegionTransform> remainingAttacks, in List<ITransformEffect> effects)
        {
            Logger.Trace($"Playing {remainingAttacks.Count} attacks: \n{string.Join('\n', remainingAttacks)}");

            while (remainingAttacks.Count > 0) {
                // Play attack

                RegionAttackRegionTransform attack = remainingAttacks[0];

                Region attacking = world.Regions[attack.targetRegionIndex];

                List<RegionAttackRegionTransform> attacksOnSameRegion;

                if (attack.isExtendedAttack) {
                    attacksOnSameRegion = new List<RegionAttackRegionTransform>() { attack };
                    remainingAttacks.RemoveAt(0);
                }
                else {
                    attacksOnSameRegion =
                        remainingAttacks.FindAll(o => o.targetRegionIndex == attack.targetRegionIndex);
                    remainingAttacks.RemoveAll(attacksOnSameRegion.Contains);
                }

                PlayAttackedRegion(in world, random, attacksOnSameRegion, effects);
            }
        }

        private void PlayConstructions(in World world, List<RegionBuildTransform> remainingBuilds, in List<ITransformEffect> effects)
        {
            Logger.Trace($"Playing {remainingBuilds.Count} builds: \n{string.Join('\n', remainingBuilds)}");
            while (remainingBuilds.Count > 0) {
                // Play attack

                RegionBuildTransform build = remainingBuilds[0];
                remainingBuilds.RemoveAt(0);

                byte supposedOwner = build.constructingRealmIndex;
                effects.Add(new ITransformEffect.ConstructionEffect() {
                    building = build.building,
                    regionIndex = build.actingRegionIndex,
                    forOwner = supposedOwner,
                    silverPricePaid = build.SilverCost,
                    isFactionHighlight = build.IsPrioritized(in world)
                });
            }
        }

        private List<RegionBuildTransform> TakeConstructions(World world, List<Transform> transforms, bool priorityOnly = false)
        {
            List<RegionBuildTransform> builds =
                   transforms
                       .Where(o => o is RegionBuildTransform build && (!priorityOnly || build.IsPrioritized(world)))
                       .Select(o => o as RegionBuildTransform)
                       .ToList();

            transforms.RemoveAll((o) => builds.Contains(o));

            builds = builds
                    .OrderBy((o) =>
                    {
                        Position position = world.Position(o.actingRegionIndex);
                        return position.SquaredDistanceWith(default);
                    })
                    .ToList();

            return builds.ToList();
        }

        private List<RegionAttackRegionTransform> TakeAttacks(World world, List<Transform> transforms)
        {

            List<RegionAttackRegionTransform> attacks =
                transforms
                    .Where(o => o is RegionAttackRegionTransform)
                    .Select(o => o as RegionAttackRegionTransform)
                    .ToList();

            transforms.RemoveAll((o) => attacks.Contains(o));

            attacks = attacks
                    .OrderBy((o) =>
                    {
                        return o.isExtendedAttack; // Extended attacks at the very last
                    })
                    .ThenBy((o) =>
                    {
                        // Other identical attacks
                        return attacks.Count((r) => r.targetRegionIndex == o.targetRegionIndex);
                    })
                    .ThenBy((o) =>
                    {
                        Position position = world.Position(o.targetRegionIndex);
                        return position.SquaredDistanceWith(default);
                    })
                    .ToList();

            return attacks;
        }

        public override string ToString()
        {
            return $"GameState ({daysPassed} days, {councilsPassed} councils)";
        }

        public void Write(BinaryWriter into)
        {
            into.Write(daysPassed);
            into.Write(councilsPassed);
            into.Write(daysRemainingBeforeNextCouncil);
            into.Write(world);
            into.Write(voting);
        }

        public void Read(byte version, BinaryReader from)
        {
            daysPassed = from.ReadInt32();
            councilsPassed = from.ReadInt32();
            daysRemainingBeforeNextCouncil = from.ReadInt32();

            world = World.Empty();
            world.Read(version, from);

            voting = new Voting(new GameRules());
            voting.Read(version, from);
        }
    }
}