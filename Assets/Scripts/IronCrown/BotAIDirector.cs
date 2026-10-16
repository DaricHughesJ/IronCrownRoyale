using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public class BotBrainState
    {
        public CombatantActor actor;
        public Vector3 destinationXZ;
        public CombatantActor targetEnemy;
        public GroundLootItem targetLoot;
        public float strafeSign = 1f;
        public float nextDecisionTime;
        public float nextShotTime;
        public float desiredYaw;
    }

    public class BotAIDirector : MonoBehaviour
    {
        private readonly List<BotBrainState> _brains = new List<BotBrainState>();

        public void InitializeBots(List<CombatantActor> allCombatants, DwarvenAirship airship)
        {
            _brains.Clear();

            for (int i = 0; i < allCombatants.Count; i++)
            {
                CombatantActor a = allCombatants[i];
                if (a.IsPlayer) continue;

                // Assign jump timing along the airship route, with clusters jumping near POIs
                float jumpT;
                if (i % 3 == 0)
                {
                    // Hot drop near middle of island / Highkeep Citadel
                    jumpT = Random.Range(0.32f, 0.68f);
                }
                else
                {
                    jumpT = Random.Range(0.08f, 0.92f);
                }
                a.PlannedJumpProgress = jumpT;

                // Choose initial landing POI or nearby drop point
                LandmarkPOI poi = MedievalCatalog.Landmarks[i % MedievalCatalog.Landmarks.Length];
                Vector2 scatter = Random.insideUnitCircle * (poi.radius * 0.65f);
                Vector3 dest = new Vector3(poi.worldXZ.x + scatter.x, 0f, poi.worldXZ.y + scatter.y);

                _brains.Add(new BotBrainState
                {
                    actor = a,
                    destinationXZ = dest,
                    strafeSign = (i % 2 == 0) ? 1f : -1f,
                    nextDecisionTime = Random.Range(0.1f, 0.8f),
                    desiredYaw = Random.Range(0f, 360f)
                });
            }
        }

        public void TickAllBots(float dt, GameMatchManager match)
        {
            float now = Time.time;
            DwarvenAirship ship = match.Airship;
            BlightZoneController zone = match.BlightZone;
            IslandWorldGenerator world = match.World;
            BotDifficulty diff = match.SelectedDifficulty;

            float accuracySpread = diff == BotDifficulty.Squire ? 5.8f : (diff == BotDifficulty.Knight ? 3.1f : 1.4f);
            float reactionDelay = diff == BotDifficulty.Squire ? 0.55f : (diff == BotDifficulty.Knight ? 0.25f : 0.08f);

            for (int i = 0; i < _brains.Count; i++)
            {
                BotBrainState b = _brains[i];
                CombatantActor bot = b.actor;
                if (bot == null || !bot.IsAlive) continue;

                // 1. Aboard Airship -> Jump at planned progress
                if (bot.CurrentDropState == DropState.AboardAirship)
                {
                    if (!ship.IsFlying || ship.FlightProgress >= bot.PlannedJumpProgress)
                    {
                        bot.JumpFromAirship(ship.transform.position);
                    }
                    continue;
                }

                // 2. Freefall / Gliding -> Steer toward chosen POI landing zone
                if (bot.CurrentDropState == DropState.Freefall || bot.CurrentDropState == DropState.Gliding)
                {
                    Vector3 toLanding = b.destinationXZ - bot.transform.position;
                    toLanding.y = 0f;
                    if (toLanding.sqrMagnitude < 4f)
                    {
                        toLanding = bot.transform.forward;
                    }
                    else
                    {
                        toLanding.Normalize();
                    }
                    float yaw = Mathf.Atan2(toLanding.x, toLanding.z) * Mathf.Rad2Deg;
                    bot.TickMovement(toLanding, yaw, false, false, dt, world);
                    continue;
                }

                // 3. Grounded Tactical AI Decision Refresh (staggered every ~0.45s per bot for 100-bot 60fps performance)
                if (now >= b.nextDecisionTime)
                {
                    b.nextDecisionTime = now + Random.Range(0.38f, 0.62f);
                    RefreshBotDecision(b, match);
                }

                // Auto-pickup any nearby ground loot within 2.9m
                TryPickupNearbyItems(bot, world);

                // Heal if low health and has alchemy item
                if (bot.Health < 52f && bot.Loadout[3] != WeaponId.None && bot.SlotCounts[3] > 0)
                {
                    bot.TryUseHealingItem();
                }

                // Outside Blight Zone priority
                bool outsideSafe = Vector2.Distance(
                    new Vector2(bot.transform.position.x, bot.transform.position.z),
                    zone.CurrentCenterXZ) > zone.CurrentRadius * 0.92f;

                if (outsideSafe && (b.targetEnemy == null || Vector3.Distance(bot.transform.position, b.targetEnemy.transform.position) > 26f))
                {
                    Vector3 toSafe = new Vector3(zone.TargetCenterXZ.x, 0f, zone.TargetCenterXZ.y) - bot.transform.position;
                    toSafe.y = 0f;
                    Vector3 dir = toSafe.sqrMagnitude > 1f ? toSafe.normalized : Vector3.forward;
                    b.desiredYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    bot.IsBlocking = false;
                    bot.IsAimingOrDrawing = false;
                    bot.TickMovement(dir, b.desiredYaw, true, false, dt, world);
                    continue;
                }

                // Combat engagement if target enemy is alive and within sight
                if (b.targetEnemy != null && b.targetEnemy.IsAlive && b.targetEnemy.CurrentDropState == DropState.Grounded)
                {
                    Vector3 toEnemy = b.targetEnemy.transform.position - bot.transform.position;
                    float dist = toEnemy.magnitude;
                    Vector3 flatDir = new Vector3(toEnemy.x, 0f, toEnemy.z);
                    if (flatDir.sqrMagnitude > 0.001f) flatDir.Normalize();

                    b.desiredYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;

                    bool hasRanged = bot.Loadout[1] != WeaponId.None && bot.ArrowAmmo > 0;
                    bool hasThrowable = bot.Loadout[2] != WeaponId.None && bot.SlotCounts[2] > 0;

                    if (dist > 8.5f && (hasRanged || (hasThrowable && dist < 30f)))
                    {
                        // Ranged / Throwable Skirmish
                        if (hasThrowable && dist < 24f && Random.value < 0.25f)
                            bot.SelectSlot(2);
                        else
                            bot.SelectSlot(1);

                        bot.IsBlocking = false;
                        bot.IsAimingOrDrawing = true;
                        bot.BowDrawCharge = Mathf.Min(1f, bot.BowDrawCharge + dt * 1.2f);

                        // Strafe laterally while keeping some distance
                        Vector3 right = new Vector3(flatDir.z, 0f, -flatDir.x) * b.strafeSign;
                        Vector3 approach = dist > 32f ? flatDir * 0.65f : (dist < 14f ? -flatDir * 0.5f : Vector3.zero);
                        Vector3 moveVec = (right * 0.75f + approach).normalized;

                        bot.TickMovement(moveVec, b.desiredYaw, false, false, dt, world);

                        if (now >= b.nextShotTime)
                        {
                            b.nextShotTime = now + Random.Range(0.85f, 1.55f) + reactionDelay;
                            b.strafeSign = -b.strafeSign;

                            WeaponSpec wSpec = MedievalCatalog.Get(bot.GetActiveWeaponId());
                            float speed = Mathf.Max(30f, wSpec.rangeOrVelocity);
                            float flightTime = dist / speed;
                            // Gravity drop compensation
                            float dropComp = 0.5f * wSpec.projectileGravity * flightTime * flightTime;
                            Vector3 aimPoint = b.targetEnemy.transform.position + Vector3.up * (1.25f + dropComp);
                            Vector3 aimOrigin = bot.transform.position + Vector3.up * 1.48f + flatDir * 0.55f;
                            Vector3 aimDir = (aimPoint - aimOrigin).normalized;

                            // Apply difficulty spread
                            aimDir = Quaternion.Euler(
                                Random.Range(-accuracySpread * 0.45f, accuracySpread * 0.45f),
                                Random.Range(-accuracySpread, accuracySpread),
                                0f) * aimDir;

                            bot.TryPerformPrimaryAction(aimOrigin, aimDir, match);
                        }
                    }
                    else
                    {
                        // Melee Charge & Duel
                        bot.SelectSlot(0);
                        bot.IsAimingOrDrawing = false;
                        WeaponSpec meleeSpec = MedievalCatalog.Get(bot.GetActiveWeaponId());
                        float attackReach = Mathf.Max(2.9f, meleeSpec.rangeOrVelocity);

                        if (dist > attackReach * 0.85f)
                        {
                            // Raise shield occasionally when closing in against ranged enemies
                            bot.IsBlocking = (dist > 6f && dist < 18f && (i % 3 == 0));
                            bot.TickMovement(flatDir, b.desiredYaw, !bot.IsBlocking, false, dt, world);
                        }
                        else
                        {
                            bot.IsBlocking = false;
                            Vector3 circleStrafe = new Vector3(flatDir.z, 0f, -flatDir.x) * b.strafeSign * 0.55f;
                            bot.TickMovement(circleStrafe, b.desiredYaw, false, false, dt, world);
                            Vector3 origin = bot.transform.position + Vector3.up * 1.2f;
                            bot.TryPerformPrimaryAction(origin, flatDir, match);
                        }
                    }
                    continue;
                }

                // 4. Looting or Patrolling toward Safe Circle
                bot.IsBlocking = false;
                bot.IsAimingOrDrawing = false;

                Vector3 goalPos = b.destinationXZ;
                if (b.targetLoot != null && !b.targetLoot.collected)
                {
                    goalPos = b.targetLoot.transform.position;
                }

                Vector3 toGoal = goalPos - bot.transform.position;
                toGoal.y = 0f;
                if (toGoal.sqrMagnitude < 4.5f)
                {
                    // Pick new patrol point inside the target safe zone
                    Vector2 rndInSafe = zone.TargetCenterXZ + Random.insideUnitCircle * (zone.TargetRadius * 0.75f);
                    b.destinationXZ = new Vector3(rndInSafe.x, 0f, rndInSafe.y);
                }
                else
                {
                    Vector3 moveDir = toGoal.normalized;
                    b.desiredYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                    bot.TickMovement(moveDir, b.desiredYaw, false, false, dt, world);
                }
            }
        }

        private void RefreshBotDecision(BotBrainState b, GameMatchManager match)
        {
            CombatantActor bot = b.actor;
            Vector3 myPos = bot.transform.position;

            // 1. Find nearest living grounded enemy within 68m
            CombatantActor closestEnemy = null;
            float bestDistSq = 68f * 68f;
            List<CombatantActor> all = match.AllCombatants;

            for (int i = 0; i < all.Count; i++)
            {
                CombatantActor other = all[i];
                if (other == null || !other.IsAlive || other == bot) continue;
                if (other.CurrentDropState != DropState.Grounded) continue;

                float dSq = (other.transform.position - myPos).sqrMagnitude;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    closestEnemy = other;
                }
            }
            b.targetEnemy = closestEnemy;

            // 2. If not in immediate close combat, look for nearby uncollected loot upgrades within 38m
            if (closestEnemy == null || bestDistSq > 25f * 25f)
            {
                GroundLootItem bestLoot = null;
                float bestLootSq = 38f * 38f;
                List<GroundLootItem> lootList = match.World.ActiveLoot;
                for (int i = 0; i < lootList.Count; i++)
                {
                    GroundLootItem item = lootList[i];
                    if (item == null || item.collected) continue;
                    float dSq = (item.transform.position - myPos).sqrMagnitude;
                    if (dSq < bestLootSq && WantsLootItem(bot, item.weaponId))
                    {
                        bestLootSq = dSq;
                        bestLoot = item;
                    }
                }
                b.targetLoot = bestLoot;
            }
        }

        private bool WantsLootItem(CombatantActor bot, WeaponId id)
        {
            WeaponSpec spec = MedievalCatalog.Get(id);
            switch (spec.slotType)
            {
                case ItemSlotType.Ranged:
                    return bot.Loadout[1] == WeaponId.None || spec.rarity > MedievalCatalog.Get(bot.Loadout[1]).rarity;
                case ItemSlotType.Melee:
                    return spec.rarity > MedievalCatalog.Get(bot.Loadout[0]).rarity;
                case ItemSlotType.ArmorUpgrade:
                    return spec.armorTierValue > bot.ArmorTier || bot.ArmorPoints < bot.MaxArmorPoints * 0.5f;
                case ItemSlotType.ArrowBundle:
                    return bot.ArrowAmmo < 36;
                case ItemSlotType.Throwable:
                    return bot.Loadout[2] == WeaponId.None || bot.SlotCounts[2] < 3;
                case ItemSlotType.Alchemy:
                    return bot.Loadout[3] == WeaponId.None || bot.SlotCounts[3] < 3;
            }
            return false;
        }

        private void TryPickupNearbyItems(CombatantActor bot, IslandWorldGenerator world)
        {
            Vector3 p = bot.transform.position;
            List<GroundLootItem> loot = world.ActiveLoot;
            for (int i = 0; i < loot.Count; i++)
            {
                GroundLootItem item = loot[i];
                if (item == null || item.collected) continue;
                if ((item.transform.position - p).sqrMagnitude <= 2.9f * 2.9f)
                {
                    if (WantsLootItem(bot, item.weaponId))
                    {
                        bot.TryPickupLoot(item);
                        break;
                    }
                }
            }
        }
    }
}
