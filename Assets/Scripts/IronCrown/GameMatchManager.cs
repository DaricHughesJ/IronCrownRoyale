using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public class GameMatchManager : MonoBehaviour
    {
        public static GameMatchManager Instance { get; private set; }

        [Header("Match Configuration")]
        public LobbySize SelectedLobbySize = LobbySize.Lobby50;
        public BotDifficulty SelectedDifficulty = BotDifficulty.Knight;
        public ControlLayoutMode SelectedControlLayout = ControlLayoutMode.Auto;

        public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Menu;
        public IslandWorldGenerator World { get; private set; }
        public DwarvenAirship Airship { get; private set; }
        public BlightZoneController BlightZone { get; private set; }
        public ProjectileManager Projectiles { get; private set; }
        public BotAIDirector BotDirector { get; private set; }
        public PlayerInputAndCamera InputCamera { get; private set; }
        public MedievalHudAndUi HudUi { get; private set; }

        public CombatantActor PlayerActor { get; private set; }
        public readonly List<CombatantActor> AllCombatants = new List<CombatantActor>();
        public readonly List<KillFeedEntry> KillFeed = new List<KillFeedEntry>();
        public readonly List<FloatingDamageText> FloatingDamages = new List<FloatingDamageText>();

        public int AliveCount { get; private set; }
        public int PlayerPlacement { get; private set; } = 1;

        private Transform _combatantsRoot;
        private int _spectateIndex;
        private float _supplyDropTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            if (FindFirstObjectByType<GameMatchManager>() == null)
            {
                GameObject go = new GameObject("IronCrown_GameMatchManager");
                go.AddComponent<GameMatchManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;

            MedievalCatalog.EnsureInitialized();
            EnsureLightingAndAtmosphere();

            if (GetComponent<MedievalAudio>() == null)
            {
                gameObject.AddComponent<MedievalAudio>();
            }

            World = GetOrAddChildComponent<IslandWorldGenerator>("IslandWorldGenerator");
            Airship = GetOrAddChildComponent<DwarvenAirship>("DwarvenSkyGalleon");
            BlightZone = GetOrAddChildComponent<BlightZoneController>("CursedBlightZone");
            Projectiles = GetOrAddChildComponent<ProjectileManager>("ProjectileManager");
            BotDirector = GetOrAddChildComponent<BotAIDirector>("BotAIDirector");
            InputCamera = GetOrAddChildComponent<PlayerInputAndCamera>("PlayerInputAndCamera");
            HudUi = GetOrAddChildComponent<MedievalHudAndUi>("MedievalHudAndUi");

            Airship.BuildShipVisual();
            Airship.gameObject.SetActive(false);

            World.GenerateWorld();
            Projectiles.InitializeProjectiles();
            InputCamera.InitializeCamera();
            HudUi.InitializeUi(this);

            CurrentPhase = MatchPhase.Menu;
        }

        private T GetOrAddChildComponent<T>(string childName) where T : Component
        {
            Transform existing = transform.Find(childName);
            if (existing != null)
            {
                T comp = existing.GetComponent<T>();
                if (comp != null) return comp;
                return existing.gameObject.AddComponent<T>();
            }
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private void EnsureLightingAndAtmosphere()
        {
            Light dirLight = FindFirstObjectByType<Light>();
            if (dirLight == null)
            {
                GameObject lightGo = new GameObject("GoldenHourSunLight");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.color = new Color(1.0f, 0.93f, 0.80f);
            dirLight.intensity = 1.25f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.transform.rotation = Quaternion.Euler(46f, -38f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.58f, 0.66f, 0.74f);
            RenderSettings.fogStartDistance = 240f;
            RenderSettings.fogEndDistance = 820f;
        }

        public void ReturnToLobbyMenu()
        {
            CurrentPhase = MatchPhase.Menu;
            Airship.gameObject.SetActive(false);
            Projectiles.ClearAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StartNewMatch(LobbySize lobbySize, BotDifficulty difficulty)
        {
            SelectedLobbySize = lobbySize;
            SelectedDifficulty = difficulty;

            KillFeed.Clear();
            FloatingDamages.Clear();
            Projectiles.ClearAll();

            // Regenerate fresh loot across the island
            World.GenerateWorld();
            HudUi.BakeMinimapTopographyTexture();

            if (_combatantsRoot != null)
            {
                Destroy(_combatantsRoot.gameObject);
            }
            _combatantsRoot = new GameObject("CombatantsRoot").transform;
            _combatantsRoot.SetParent(transform, false);
            AllCombatants.Clear();

            Airship.LaunchNewFlightPath();
            BlightZone.InitializeZone();

            int totalCount = (int)lobbySize;
            AliveCount = totalCount;
            PlayerPlacement = totalCount;
            _supplyDropTimer = 42f;

            // 1. Spawn Human Player (Actor 0)
            GameObject playerGo = new GameObject("Player_Knight");
            playerGo.transform.SetParent(_combatantsRoot, false);
            PlayerActor = playerGo.AddComponent<CombatantActor>();
            PlayerActor.InitializeActor(0, "You (Iron Knight)", true, new Color(0.82f, 0.20f, 0.18f));
            PlayerActor.transform.position = Airship.StartPoint;
            AllCombatants.Add(PlayerActor);

            // 2. Spawn 24, 49, or 99 Bot Combatants
            for (int i = 1; i < totalCount; i++)
            {
                string baseName = MedievalCatalog.BotNames[(i - 1) % MedievalCatalog.BotNames.Length];
                string botName = totalCount > MedievalCatalog.BotNames.Length
                    ? $"{baseName} #{i}"
                    : baseName;
                Color col = MedievalCatalog.HeraldicColors[i % MedievalCatalog.HeraldicColors.Length];

                GameObject botGo = new GameObject($"Bot_{i}_{baseName}");
                botGo.transform.SetParent(_combatantsRoot, false);
                CombatantActor bot = botGo.AddComponent<CombatantActor>();
                bot.InitializeActor(i, botName, false, col);
                bot.transform.position = Airship.StartPoint;
                AllCombatants.Add(bot);
            }

            BotDirector.InitializeBots(AllCombatants, Airship);

            Vector3 shipDir = (Airship.EndPoint - Airship.StartPoint).normalized;
            float startYaw = Mathf.Atan2(shipDir.x, shipDir.z) * Mathf.Rad2Deg;
            InputCamera.ResetViewAngles(startYaw);

            CurrentPhase = MatchPhase.InProgress;
            MedievalAudio.PlayWarHorn();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (CurrentPhase == MatchPhase.Menu)
            {
                InputCamera.TickPlayerAndCamera(dt, this);
                return;
            }

            Airship.TickAirship(dt);
            BlightZone.TickZone(dt);

            // Periodic Royal Griffin Supply Drop
            _supplyDropTimer -= dt;
            if (_supplyDropTimer <= 0f)
            {
                _supplyDropTimer = 50f;
                World.SpawnRoyalSupplyDrop(BlightZone.TargetCenterXZ, BlightZone.TargetRadius);
            }

            // Tick Player & Camera
            InputCamera.TickPlayerAndCamera(dt, this);

            // Tick all Bots
            BotDirector.TickAllBots(dt, this);

            // Tick Projectiles & Wildfire Hazards
            Projectiles.TickProjectiles(dt, this);

            // Tick Blight Storm damage on all combatants
            for (int i = 0; i < AllCombatants.Count; i++)
            {
                CombatantActor c = AllCombatants[i];
                if (c != null && c.IsAlive)
                {
                    c.TickZoneAndFireHazards(dt, BlightZone, this);
                }
            }
        }

        public void ExecuteMeleeCleave(CombatantActor attacker, WeaponSpec weapon)
        {
            Vector3 origin = attacker.transform.position;
            Vector3 forward = attacker.transform.forward;
            float maxDist = weapon.rangeOrVelocity;
            float cosHalfAngle = Mathf.Cos(weapon.cleaveAngle * 0.5f * Mathf.Deg2Rad);

            for (int i = 0; i < AllCombatants.Count; i++)
            {
                CombatantActor target = AllCombatants[i];
                if (target == null || !target.IsAlive || target == attacker) continue;
                if (target.CurrentDropState != DropState.Grounded) continue;

                Vector3 delta = target.transform.position - origin;
                if (Mathf.Abs(delta.y) > 2.4f) continue;

                Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
                float dist = flatDelta.magnitude;
                if (dist > maxDist) continue;

                Vector3 dir = dist > 0.01f ? (flatDelta / dist) : forward;
                if (dist < 1.1f || Vector3.Dot(forward, dir) >= cosHalfAngle)
                {
                    bool ignoreShield = weapon.id == WeaponId.RoyalHalberd || weapon.id == WeaponId.SunforgedGreatsword;
                    target.ApplyDamage(weapon.baseDamage, attacker, weapon.displayName, false, ignoreShield, this);
                }
            }
        }

        public void RecordFloatingDamage(Vector3 worldPos, int amount, bool isHeadshot, bool isArmorHit)
        {
            FloatingDamages.Add(new FloatingDamageText
            {
                worldPos = worldPos,
                amount = amount,
                isHeadshot = isHeadshot,
                isArmorHit = isArmorHit,
                spawnTime = Time.time
            });
            HudUi.NotifyHitMarker(isHeadshot);
        }

        public void OnCombatantEliminated(CombatantActor victim, CombatantActor killer, string weaponName)
        {
            if (killer != null && killer != victim)
            {
                killer.Kills++;
            }

            // Drop victim's best weapon & a healing draught on the ground
            Vector3 dropPos = victim.transform.position;
            dropPos.y = World.GetGroundHeight(dropPos.x, dropPos.z);

            if (victim.Loadout[1] != WeaponId.None)
                World.SpawnLootAt(dropPos + new Vector3(0.75f, 0f, 0f), victim.Loadout[1], 1);
            else if (victim.Loadout[0] != WeaponId.OakenClub && victim.Loadout[0] != WeaponId.None)
                World.SpawnLootAt(dropPos + new Vector3(0.75f, 0f, 0f), victim.Loadout[0], 1);

            World.SpawnLootAt(dropPos + new Vector3(-0.75f, 0f, 0f), WeaponId.ApothecaryVial, 1);

            float dist = (killer != null) ? Vector3.Distance(killer.transform.position, victim.transform.position) : 0f;
            KillFeed.Add(new KillFeedEntry
            {
                killerName = killer != null ? killer.DisplayName : "Blight Storm",
                victimName = victim.DisplayName,
                weaponName = weaponName,
                distance = dist,
                timestamp = Time.time,
                killerIsPlayer = killer != null && killer.IsPlayer,
                victimIsPlayer = victim.IsPlayer
            });

            // Recompute alive count
            int alive = 0;
            CombatantActor lastStanding = null;
            for (int i = 0; i < AllCombatants.Count; i++)
            {
                if (AllCombatants[i] != null && AllCombatants[i].IsAlive)
                {
                    alive++;
                    lastStanding = AllCombatants[i];
                }
            }
            AliveCount = alive;

            if (victim.IsPlayer)
            {
                PlayerPlacement = alive + 1;
                CurrentPhase = MatchPhase.Defeat;
                CycleSpectateTarget();
            }
            else if (alive <= 1)
            {
                if (PlayerActor != null && PlayerActor.IsAlive)
                {
                    PlayerPlacement = 1;
                    CurrentPhase = MatchPhase.Victory;
                    MedievalAudio.PlayWarHorn();
                }
                else
                {
                    CurrentPhase = MatchPhase.Defeat;
                }
            }
        }

        public CombatantActor GetSpectateTarget()
        {
            if (AllCombatants.Count == 0) return null;
            for (int offset = 0; offset < AllCombatants.Count; offset++)
            {
                int idx = (_spectateIndex + offset) % AllCombatants.Count;
                CombatantActor c = AllCombatants[idx];
                if (c != null && c.IsAlive)
                {
                    _spectateIndex = idx;
                    return c;
                }
            }
            return PlayerActor;
        }

        public void CycleSpectateTarget()
        {
            _spectateIndex = (_spectateIndex + 1) % Mathf.Max(1, AllCombatants.Count);
            GetSpectateTarget();
        }
    }
}
