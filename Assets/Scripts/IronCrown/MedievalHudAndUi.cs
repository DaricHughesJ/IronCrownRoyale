using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public class MedievalHudAndUi : MonoBehaviour
    {
        private GameMatchManager _match;

        // Cached 1x1 solid textures for crisp medieval UI borders and fills
        private Texture2D _whiteTex;
        private Texture2D _minimapTex;
        private Color32[] _minimapBasePixels;
        private Color32[] _minimapFramePixels;
        private const int MapTexSize = 192;

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _monoBoldStyle;
        private GUIStyle _centerBannerStyle;
        private GUIStyle _killFeedStyle;
        private GUIStyle _poiLabelStyle;
        private GUIStyle _damageNumberStyle;

        // Mobile Virtual Joystick & Touch state
        private bool _joyDragging;
        private Vector2 _joyCenter;
        private Vector2 _joyCurrent;
        private bool _lookDragging;
        private Vector2 _lastLookPos;

        // Hitmarker feedback
        private float _lastHitMarkerTime = -10f;
        private bool _lastHitWasHeadshot;

        // Palette (Unslop-UI compliant: Wrought Iron, Aged Parchment, Burnished Brass, Heraldic Crimson, Apothecary Emerald, Blight Amber)
        private static readonly Color ColIronDark = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        private static readonly Color ColIronPanel = new Color(0.13f, 0.11f, 0.09f, 0.88f);
        private static readonly Color ColBrassBorder = new Color(0.78f, 0.61f, 0.32f, 0.95f);
        private static readonly Color ColParchment = new Color(0.92f, 0.86f, 0.75f, 1.0f);
        private static readonly Color ColParchmentMuted = new Color(0.72f, 0.66f, 0.56f, 1.0f);
        private static readonly Color ColCrimson = new Color(0.66f, 0.16f, 0.17f, 0.95f);
        private static readonly Color ColCrimsonBright = new Color(0.86f, 0.24f, 0.22f, 1.0f);
        private static readonly Color ColEmerald = new Color(0.22f, 0.48f, 0.26f, 0.95f);
        private static readonly Color ColBlightAmber = new Color(0.88f, 0.48f, 0.14f, 0.95f);
        private static readonly Color ColArmorBlue = new Color(0.28f, 0.54f, 0.78f, 0.95f);

        public void InitializeUi(GameMatchManager match)
        {
            _match = match;
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();

            BakeMinimapTopographyTexture();
        }

        public void NotifyHitMarker(bool headshot)
        {
            _lastHitMarkerTime = Time.time;
            _lastHitWasHeadshot = headshot;
        }

        public void BakeMinimapTopographyTexture()
        {
            if (_minimapTex == null)
            {
                _minimapTex = new Texture2D(MapTexSize, MapTexSize, TextureFormat.RGBA32, false);
                _minimapTex.wrapMode = TextureWrapMode.Clamp;
                _minimapTex.filterMode = FilterMode.Bilinear;
            }

            _minimapBasePixels = new Color32[MapTexSize * MapTexSize];
            _minimapFramePixels = new Color32[MapTexSize * MapTexSize];

            float worldHalf = MedievalCatalog.IslandHalfExtent;

            for (int py = 0; py < MapTexSize; py++)
            {
                float wz = Mathf.Lerp(-worldHalf, worldHalf, (float)py / (MapTexSize - 1));
                for (int px = 0; px < MapTexSize; px++)
                {
                    float wx = Mathf.Lerp(-worldHalf, worldHalf, (float)px / (MapTexSize - 1));
                    float h = IslandWorldGenerator.SampleProceduralHeight(wx, wz);

                    Color c;
                    if (h <= MedievalCatalog.WaterLevel + 0.3f)
                        c = new Color(0.12f, 0.22f, 0.30f);
                    else if (h < 7.0f)
                        c = new Color(0.72f, 0.64f, 0.46f);
                    else if (h < 16f)
                        c = new Color(0.28f, 0.39f, 0.22f);
                    else
                        c = new Color(0.42f, 0.40f, 0.35f);

                    // Subtle contour lines
                    if (h > 6.5f && Mathf.Abs((h % 5f) - 2.5f) < 0.18f)
                    {
                        c = Color.Lerp(c, Color.black, 0.16f);
                    }

                    _minimapBasePixels[py * MapTexSize + px] = c;
                }
            }

            // Stamp POI footprints onto base map
            for (int i = 0; i < MedievalCatalog.Landmarks.Length; i++)
            {
                LandmarkPOI poi = MedievalCatalog.Landmarks[i];
                Vector2Int uv = WorldToMapPixel(poi.worldXZ.x, poi.worldXZ.y);
                int rPx = Mathf.RoundToInt((poi.radius / (worldHalf * 2f)) * MapTexSize * 0.45f);
                DrawFilledSquareOnPixels(_minimapBasePixels, uv.x, uv.y, Mathf.Max(3, rPx), new Color32(165, 138, 95, 255));
            }

            RefreshMinimapOverlay();
        }

        private float _lastMinimapRefreshTime;

        private void RefreshMinimapOverlay()
        {
            if (_minimapBasePixels == null || _match == null) return;
            System.Array.Copy(_minimapBasePixels, _minimapFramePixels, _minimapBasePixels.Length);

            BlightZoneController zone = _match.BlightZone;
            if (zone != null)
            {
                // Draw Target Safe Circle (parchment white ring)
                Vector2Int safeC = WorldToMapPixel(zone.TargetCenterXZ.x, zone.TargetCenterXZ.y);
                int safeR = Mathf.RoundToInt((zone.TargetRadius / (MedievalCatalog.IslandHalfExtent * 2f)) * MapTexSize);
                DrawCircleOnPixels(_minimapFramePixels, safeC.x, safeC.y, safeR, new Color32(238, 228, 202, 255));

                // Draw Current Blight Storm Wall (amber/crimson ring)
                Vector2Int curC = WorldToMapPixel(zone.CurrentCenterXZ.x, zone.CurrentCenterXZ.y);
                int curR = Mathf.RoundToInt((zone.CurrentRadius / (MedievalCatalog.IslandHalfExtent * 2f)) * MapTexSize);
                DrawCircleOnPixels(_minimapFramePixels, curC.x, curC.y, curR, new Color32(235, 95, 35, 255));
                DrawCircleOnPixels(_minimapFramePixels, curC.x, curC.y, Mathf.Max(1, curR - 1), new Color32(235, 95, 35, 255));
            }

            // Draw Airship Flight Line if flying
            DwarvenAirship ship = _match.Airship;
            if (ship != null && ship.IsFlying)
            {
                Vector2Int p0 = WorldToMapPixel(ship.StartPoint.x, ship.StartPoint.z);
                Vector2Int p1 = WorldToMapPixel(ship.EndPoint.x, ship.EndPoint.z);
                DrawLineOnPixels(_minimapFramePixels, p0.x, p0.y, p1.x, p1.y, new Color32(220, 185, 95, 220));

                Vector2Int shipPx = WorldToMapPixel(ship.transform.position.x, ship.transform.position.z);
                DrawFilledSquareOnPixels(_minimapFramePixels, shipPx.x, shipPx.y, 3, new Color32(250, 210, 80, 255));
            }

            // Draw Royal Griffin Supply Drops
            if (_match.World != null)
            {
                var drops = _match.World.SupplyDrops;
                for (int i = 0; i < drops.Count; i++)
                {
                    if (drops[i] != null)
                    {
                        Vector2Int dp = WorldToMapPixel(drops[i].targetGroundPos.x, drops[i].targetGroundPos.z);
                        DrawFilledSquareOnPixels(_minimapFramePixels, dp.x, dp.y, 3, new Color32(235, 55, 55, 255));
                    }
                }
            }

            // Draw Player Marker (Gold + Crimson Center)
            CombatantActor focusActor = (_match.PlayerActor != null && _match.PlayerActor.IsAlive)
                ? _match.PlayerActor
                : _match.GetSpectateTarget();

            if (focusActor != null)
            {
                Vector2Int pp = WorldToMapPixel(focusActor.transform.position.x, focusActor.transform.position.z);
                DrawFilledSquareOnPixels(_minimapFramePixels, pp.x, pp.y, 4, new Color32(20, 18, 15, 255));
                DrawFilledSquareOnPixels(_minimapFramePixels, pp.x, pp.y, 2, new Color32(250, 212, 75, 255));
            }

            _minimapTex.SetPixels32(_minimapFramePixels);
            _minimapTex.Apply(false);
        }

        private Vector2Int WorldToMapPixel(float wx, float wz)
        {
            float worldHalf = MedievalCatalog.IslandHalfExtent;
            int px = Mathf.Clamp(Mathf.RoundToInt(((wx + worldHalf) / (worldHalf * 2f)) * (MapTexSize - 1)), 0, MapTexSize - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(((wz + worldHalf) / (worldHalf * 2f)) * (MapTexSize - 1)), 0, MapTexSize - 1);
            return new Vector2Int(px, py);
        }

        private void DrawFilledSquareOnPixels(Color32[] buf, int cx, int cy, int halfSize, Color32 col)
        {
            for (int y = cy - halfSize; y <= cy + halfSize; y++)
            {
                if (y < 0 || y >= MapTexSize) continue;
                for (int x = cx - halfSize; x <= cx + halfSize; x++)
                {
                    if (x < 0 || x >= MapTexSize) continue;
                    buf[y * MapTexSize + x] = col;
                }
            }
        }

        private void DrawCircleOnPixels(Color32[] buf, int cx, int cy, int radius, Color32 col)
        {
            if (radius <= 0) return;
            int steps = Mathf.Max(64, radius * 6);
            for (int i = 0; i < steps; i++)
            {
                float a = (i / (float)steps) * Mathf.PI * 2f;
                int x = cx + Mathf.RoundToInt(Mathf.Cos(a) * radius);
                int y = cy + Mathf.RoundToInt(Mathf.Sin(a) * radius);
                if (x >= 0 && x < MapTexSize && y >= 0 && y < MapTexSize)
                {
                    buf[y * MapTexSize + x] = col;
                }
            }
        }

        private void DrawLineOnPixels(Color32[] buf, int x0, int y0, int x1, int y1, Color32 col)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                if (i % 3 == 1) continue; // Dashed medieval route line
                float t = (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (x >= 0 && x < MapTexSize && y >= 0 && y < MapTexSize)
                {
                    buf[y * MapTexSize + x] = col;
                }
            }
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = ColParchment;

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _subtitleStyle.normal.textColor = ColBrassBorder;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = ColParchment;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            _bodyStyle.normal.textColor = ColParchmentMuted;

            _monoBoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _monoBoldStyle.normal.textColor = ColParchment;

            _centerBannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _centerBannerStyle.normal.textColor = ColParchment;

            _killFeedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _killFeedStyle.normal.textColor = ColParchment;

            _poiLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _poiLabelStyle.normal.textColor = ColParchment;

            _damageNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _damageNumberStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (_match == null) return;
            EnsureStyles();

            if (Time.time - _lastMinimapRefreshTime > 0.15f)
            {
                _lastMinimapRefreshTime = Time.time;
                RefreshMinimapOverlay();
            }

            if (_match.CurrentPhase == MatchPhase.Menu)
            {
                DrawPreMatchLobbyMenu();
                return;
            }

            DrawWorldFloatingDamageNumbers();
            DrawCenterReticleAndDropPrompt();
            DrawTopLeftKillFeedAndControlsHint();
            DrawTopRightMinimapAndCounters();
            DrawBottomVitalsAndHotbar();
            DrawNearbyLootPickupCard();

            if (_match.InputCamera.ShouldShowMobileTouchHud(_match.SelectedControlLayout) && !_match.InputCamera.IsMapOpen)
            {
                DrawMobileTouchControlsOverlay();
            }

            if (_match.InputCamera.IsMapOpen)
            {
                DrawFullParchmentMapModal();
            }

            if (_match.CurrentPhase == MatchPhase.Victory || _match.CurrentPhase == MatchPhase.Defeat || (_match.PlayerActor != null && !_match.PlayerActor.IsAlive))
            {
                DrawEndOfMatchOrSpectateOverlay();
            }
        }

        private void DrawPreMatchLobbyMenu()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float panelW = Mathf.Min(680f, sw - 32f);
            float panelH = Mathf.Min(560f, sh - 32f);
            Rect panel = new Rect((sw - panelW) * 0.5f, (sh - panelH) * 0.5f, panelW, panelH);

            DrawMedievalPanel(panel, ColIronDark, ColBrassBorder, 2f);

            GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 38f), "IRON CROWN", _titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 52f, panel.width, 24f), "MEDIEVAL BATTLE ROYALE  —  ISLAND OF HIGHKEEP", _subtitleStyle);

            float y = panel.y + 92f;
            float padX = panel.x + 34f;
            float innerW = panel.width - 68f;

            // 1. LOBBY SIZE SELECTOR (25 / 50 / 100)
            GUI.Label(new Rect(padX, y, innerW, 24f), "1. SELECT GARRISON LOBBY SIZE (PLAYER + BOTS)", _headerStyle);
            y += 28f;
            float btnW3 = (innerW - 20f) / 3f;
            if (DrawMedievalButton(new Rect(padX, y, btnW3, 44f), "25 COMBATANTS\n(Skirmish)", _match.SelectedLobbySize == LobbySize.Lobby25))
                _match.SelectedLobbySize = LobbySize.Lobby25;
            if (DrawMedievalButton(new Rect(padX + btnW3 + 10f, y, btnW3, 44f), "50 COMBATANTS\n(Grand Melee)", _match.SelectedLobbySize == LobbySize.Lobby50))
                _match.SelectedLobbySize = LobbySize.Lobby50;
            if (DrawMedievalButton(new Rect(padX + (btnW3 + 10f) * 2f, y, btnW3, 44f), "100 COMBATANTS\n(Royal Crusade)", _match.SelectedLobbySize == LobbySize.Lobby100))
                _match.SelectedLobbySize = LobbySize.Lobby100;

            y += 62f;

            // 2. BOT AI DIFFICULTY SELECTOR
            GUI.Label(new Rect(padX, y, innerW, 24f), "2. SELECT BOT COMBATANT PROWESS", _headerStyle);
            y += 28f;
            if (DrawMedievalButton(new Rect(padX, y, btnW3, 40f), "SQUIRE (Casual)", _match.SelectedDifficulty == BotDifficulty.Squire))
                _match.SelectedDifficulty = BotDifficulty.Squire;
            if (DrawMedievalButton(new Rect(padX + btnW3 + 10f, y, btnW3, 40f), "KNIGHT (Standard)", _match.SelectedDifficulty == BotDifficulty.Knight))
                _match.SelectedDifficulty = BotDifficulty.Knight;
            if (DrawMedievalButton(new Rect(padX + (btnW3 + 10f) * 2f, y, btnW3, 40f), "WARLORD (Veteran)", _match.SelectedDifficulty == BotDifficulty.Warlord))
                _match.SelectedDifficulty = BotDifficulty.Warlord;

            y += 58f;

            // 3. PLATFORM CONTROL MODE (PC vs MOBILE TOUCH HUD)
            GUI.Label(new Rect(padX, y, innerW, 24f), "3. SELECT INPUT & HUD LAYOUT (PC / MOBILE)", _headerStyle);
            y += 28f;
            if (DrawMedievalButton(new Rect(padX, y, btnW3, 40f), "AUTO DETECT", _match.SelectedControlLayout == ControlLayoutMode.Auto))
                _match.SelectedControlLayout = ControlLayoutMode.Auto;
            if (DrawMedievalButton(new Rect(padX + btnW3 + 10f, y, btnW3, 40f), "PC (Mouse + WASD)", _match.SelectedControlLayout == ControlLayoutMode.PCKeyboardMouse))
                _match.SelectedControlLayout = ControlLayoutMode.PCKeyboardMouse;
            if (DrawMedievalButton(new Rect(padX + (btnW3 + 10f) * 2f, y, btnW3, 40f), "MOBILE TOUCH HUD", _match.SelectedControlLayout == ControlLayoutMode.MobileTouch))
                _match.SelectedControlLayout = ControlLayoutMode.MobileTouch;

            y += 58f;

            // Brief Arsenal & Rules summary box
            Rect infoRect = new Rect(padX, y, innerW, 96f);
            DrawMedievalPanel(infoRect, ColIronPanel, ColParchmentMuted, 1f);
            string rules =
                "• DROP: Leap from the Dwarven Sky-Galleon ([SPACE] / DIVE) and unfurl your canvas glider over 6 medieval strongholds.\n" +
                "• ARSENAL: Longbows, Siege Arbalests, Dwarven Repeaters, Swords, Halberds, Greatswords, Wildfire Flasks, & Tier I–III Plate Armor.\n" +
                "• PC KEYS: [WASD] Move | [Shift] Sprint | [LMB] Attack/Fire | [RMB] Aim/Shield Block | [1-4] Slots | [E] Loot | [V] 1st/3rd Person | [M] Map";
            GUI.Label(new Rect(infoRect.x + 12f, infoRect.y + 8f, infoRect.width - 24f, infoRect.height - 16f), rules, _bodyStyle);

            y += 112f;

            // Launch Match CTA
            Rect launchRect = new Rect(padX, y, innerW, 52f);
            if (DrawMedievalActionButton(launchRect, $"BOARD DWARVEN AIRSHIP  —  LAUNCH {(int)_match.SelectedLobbySize}-PLAYER BATTLE ROYALE", ColCrimson))
            {
                _match.StartNewMatch(_match.SelectedLobbySize, _match.SelectedDifficulty);
            }
        }

        private void DrawTopRightMinimapAndCounters()
        {
            float sw = Screen.width;
            float mapBoxSize = 186f;
            Rect mapRect = new Rect(sw - mapBoxSize - 16f, 16f, mapBoxSize, mapBoxSize);

            // Top telemetry badges to the left of Minimap
            Rect aliveBadge = new Rect(mapRect.x - 190f, 16f, 88f, 44f);
            DrawMedievalPanel(aliveBadge, ColIronDark, ColBrassBorder, 1.5f);
            GUI.Label(new Rect(aliveBadge.x, aliveBadge.y + 3f, aliveBadge.width, 16f), "ALIVE", _poiLabelStyle);
            GUI.Label(new Rect(aliveBadge.x, aliveBadge.y + 18f, aliveBadge.width, 22f), $"{_match.AliveCount} / {(int)_match.SelectedLobbySize}", _monoBoldStyle);

            Rect killsBadge = new Rect(mapRect.x - 96f, 16f, 86f, 44f);
            DrawMedievalPanel(killsBadge, ColIronDark, ColCrimson, 1.5f);
            GUI.Label(new Rect(killsBadge.x, killsBadge.y + 3f, killsBadge.width, 16f), "KILLS", _poiLabelStyle);
            int kills = _match.PlayerActor != null ? _match.PlayerActor.Kills : 0;
            GUI.Label(new Rect(killsBadge.x, killsBadge.y + 18f, killsBadge.width, 22f), kills.ToString(), _monoBoldStyle);

            // Zone Timer Bar below Alive/Kills badges
            BlightZoneController zone = _match.BlightZone;
            if (zone != null)
            {
                Rect zoneBox = new Rect(mapRect.x - 190f, 66f, 180f, 42f);
                Color borderCol = zone.IsShrinking ? ColBlightAmber : ColBrassBorder;
                DrawMedievalPanel(zoneBox, ColIronDark, borderCol, 1.5f);

                int secs = Mathf.Max(0, Mathf.CeilToInt(zone.PhaseTimer));
                string status = zone.IsShrinking ? $"BLIGHT CLOSING ({secs}s)" : $"BLIGHT STAGE {zone.CurrentStage + 1} ({secs}s)";
                GUI.Label(new Rect(zoneBox.x, zoneBox.y + 4f, zoneBox.width, 18f), status, _poiLabelStyle);

                float prog = 1f - Mathf.Clamp01(zone.PhaseTimer / Mathf.Max(1f, zone.PhaseDuration));
                DrawProgressBar(new Rect(zoneBox.x + 8f, zoneBox.y + 25f, zoneBox.width - 16f, 10f), prog, zone.IsShrinking ? ColBlightAmber : ColBrassBorder);
            }

            // Camera View & Map Quick Buttons (great for both PC and Mobile!)
            Rect camBtn = new Rect(mapRect.x - 190f, 114f, 88f, 32f);
            string camText = _match.InputCamera.FirstPersonMode ? "VIEW: 1ST" : "VIEW: 3RD";
            if (DrawMedievalButton(camBtn, camText, _match.InputCamera.FirstPersonMode))
            {
                _match.InputCamera.FirstPersonMode = !_match.InputCamera.FirstPersonMode;
            }

            Rect mapBtn = new Rect(mapRect.x - 96f, 114f, 86f, 32f);
            if (DrawMedievalButton(mapBtn, "MAP [M]", _match.InputCamera.IsMapOpen))
            {
                _match.InputCamera.IsMapOpen = !_match.InputCamera.IsMapOpen;
            }

            Rect menuBtn = new Rect(mapRect.x - 190f, 152f, 180f, 28f);
            if (DrawMedievalButton(menuBtn, "LOBBY / SETTINGS", false))
            {
                _match.ReturnToLobbyMenu();
            }

            // Minimap Frame & Texture
            DrawMedievalPanel(mapRect, ColIronDark, ColBrassBorder, 2f);
            if (_minimapTex != null)
            {
                Rect innerMap = new Rect(mapRect.x + 4f, mapRect.y + 4f, mapRect.width - 8f, mapRect.height - 8f);
                GUI.DrawTexture(innerMap, _minimapTex);
                if (GUI.Button(innerMap, GUIContent.none, GUIStyle.none))
                {
                    _match.InputCamera.IsMapOpen = !_match.InputCamera.IsMapOpen;
                }
            }
            GUI.Label(new Rect(mapRect.x, mapRect.yMax - 20f, mapRect.width, 18f), "TAP OR [M] FOR FULL MAP", _poiLabelStyle);
        }

        private void DrawTopLeftKillFeedAndControlsHint()
        {
            Rect feedBox = new Rect(16f, 16f, 340f, 26f);
            List<KillFeedEntry> feed = _match.KillFeed;
            float now = Time.time;

            int shown = 0;
            for (int i = feed.Count - 1; i >= 0 && shown < 5; i--)
            {
                KillFeedEntry e = feed[i];
                if (now - e.timestamp > 12f) continue;

                Rect row = new Rect(feedBox.x, feedBox.y + shown * 26f, feedBox.width, 24f);
                DrawMedievalPanel(row, new Color(0.08f, 0.07f, 0.06f, 0.78f), e.killerIsPlayer ? ColBrassBorder : new Color(0.4f, 0.35f, 0.28f, 0.6f), 1f);
                string distTxt = e.distance >= 5f ? $" ({Mathf.RoundToInt(e.distance)}m)" : "";
                string line = $"{e.killerName} [{e.weaponName}] -> {e.victimName}{distTxt}";
                GUI.Label(new Rect(row.x + 8f, row.y, row.width - 14f, row.height), line, _killFeedStyle);
                shown++;
            }
        }

        private void DrawCenterReticleAndDropPrompt()
        {
            CombatantActor player = _match.PlayerActor;
            if (player == null || !player.IsAlive) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            // Airship / Freefall / Gliding Banner
            if (player.CurrentDropState == DropState.AboardAirship)
            {
                Rect banner = new Rect(cx - 240f, 82f, 480f, 54f);
                DrawMedievalPanel(banner, ColIronDark, ColBrassBorder, 2f);
                GUI.Label(new Rect(banner.x, banner.y + 6f, banner.width, 22f), "ABOARD DWARVEN SKY-GALLEON", _subtitleStyle);
                GUI.Label(new Rect(banner.x, banner.y + 26f, banner.width, 22f), "PRESS [SPACE] OR TAP [DIVE] TO LEAP ONTO THE ISLAND", _monoBoldStyle);
                return;
            }
            else if (player.CurrentDropState == DropState.Freefall || player.CurrentDropState == DropState.Gliding)
            {
                float groundY = _match.World.GetGroundHeight(player.transform.position.x, player.transform.position.z);
                int alt = Mathf.Max(0, Mathf.RoundToInt(player.transform.position.y - groundY));
                Rect banner = new Rect(cx - 210f, 82f, 420f, 46f);
                DrawMedievalPanel(banner, ColIronDark, ColBrassBorder, 1.5f);
                string modeTxt = player.CurrentDropState == DropState.Freefall
                    ? $"FREEFALLING — ALTITUDE: {alt}m ([SPACE] TO OPEN GLIDER)"
                    : $"CANVAS GLIDER DEPLOYED — ALTITUDE: {alt}m";
                GUI.Label(banner, modeTxt, _monoBoldStyle);
            }

            // Blight Storm Warning Banner
            if (_match.BlightZone != null && _match.BlightZone.IsOutsideZone(player.transform.position))
            {
                Rect warn = new Rect(cx - 220f, 24f, 440f, 34f);
                DrawMedievalPanel(warn, new Color(0.55f, 0.14f, 0.08f, 0.90f), ColBlightAmber, 1.5f);
                GUI.Label(warn, "OUTSIDE SAFE RING — TAKING CURSED BLIGHT DAMAGE!", _monoBoldStyle);
            }

            if (_match.InputCamera.IsMapOpen) return;

            // Crosshair Reticle
            WeaponSpec activeSpec = MedievalCatalog.Get(player.GetActiveWeaponId());
            float spread = player.IsAimingOrDrawing ? 6f : 11f;

            DrawSolidRect(new Rect(cx - 2f, cy - 2f, 4f, 4f), ColParchment);
            DrawSolidRect(new Rect(cx - spread - 8f, cy - 1f, 8f, 2f), ColBrassBorder);
            DrawSolidRect(new Rect(cx + spread, cy - 1f, 8f, 2f), ColBrassBorder);
            DrawSolidRect(new Rect(cx - 1f, cy - spread - 8f, 2f, 8f), ColBrassBorder);
            DrawSolidRect(new Rect(cx - 1f, cy + spread, 2f, 8f), ColBrassBorder);

            // Ranged Bow / Crossbow Ballistic Drop Ladder Ticks & Draw Charge Meter
            if (activeSpec.slotType == ItemSlotType.Ranged || activeSpec.slotType == ItemSlotType.Throwable)
            {
                DrawSolidRect(new Rect(cx - 5f, cy + 16f, 10f, 2f), ColParchmentMuted);
                DrawSolidRect(new Rect(cx - 4f, cy + 28f, 8f, 2f), ColParchmentMuted);
                DrawSolidRect(new Rect(cx - 3f, cy + 42f, 6f, 2f), ColParchmentMuted);

                if (player.BowDrawCharge > 0.02f)
                {
                    Rect chargeBar = new Rect(cx - 36f, cy + 52f, 72f, 7f);
                    DrawProgressBar(chargeBar, player.BowDrawCharge, ColBrassBorder);
                }
            }

            // Hitmarker X flash when landing hits
            if (Time.time - _lastHitMarkerTime < 0.24f)
            {
                Color hmCol = _lastHitWasHeadshot ? ColCrimsonBright : ColBrassBorder;
                DrawSolidRect(new Rect(cx - 10f, cy - 10f, 6f, 6f), hmCol);
                DrawSolidRect(new Rect(cx + 4f, cy - 10f, 6f, 6f), hmCol);
                DrawSolidRect(new Rect(cx - 10f, cy + 4f, 6f, 6f), hmCol);
                DrawSolidRect(new Rect(cx + 4f, cy + 4f, 6f, 6f), hmCol);
            }
        }

        private void DrawBottomVitalsAndHotbar()
        {
            CombatantActor actor = (_match.PlayerActor != null && _match.PlayerActor.IsAlive)
                ? _match.PlayerActor
                : _match.GetSpectateTarget();
            if (actor == null) return;

            float sw = Screen.width;
            float sh = Screen.height;
            float barW = 396f;
            float startX = (sw - barW) * 0.5f;
            float baseY = sh - 134f;

            // Vitals Panel (HP, Armor Tier, Stamina, Arrows)
            Rect vitalsBox = new Rect(startX, baseY, barW, 56f);
            DrawMedievalPanel(vitalsBox, ColIronDark, ColBrassBorder, 1.5f);

            // Health Bar
            Rect hpRect = new Rect(vitalsBox.x + 10f, vitalsBox.y + 8f, 240f, 18f);
            DrawProgressBar(hpRect, actor.Health / Mathf.Max(1f, actor.MaxHealth), ColCrimson);
            GUI.Label(hpRect, $"VITALITY: {Mathf.CeilToInt(actor.Health)} / {Mathf.RoundToInt(actor.MaxHealth)}", _poiLabelStyle);

            // Armor Bar
            Rect armorRect = new Rect(vitalsBox.x + 10f, vitalsBox.y + 30f, 165f, 16f);
            float armorNorm = actor.MaxArmorPoints > 0f ? (actor.ArmorPoints / actor.MaxArmorPoints) : 0f;
            DrawProgressBar(armorRect, armorNorm, ColArmorBlue);
            string armorTxt = actor.ArmorTier > 0
                ? $"{MedievalCatalog.GetArmorTierName(actor.ArmorTier)} ({Mathf.CeilToInt(actor.ArmorPoints)})"
                : "No Armor (Tunic)";
            GUI.Label(armorRect, armorTxt, _poiLabelStyle);

            // Stamina Bar
            Rect stamRect = new Rect(vitalsBox.x + 182f, vitalsBox.y + 30f, 68f, 16f);
            DrawProgressBar(stamRect, actor.Stamina / Mathf.Max(1f, actor.MaxStamina), ColEmerald);
            GUI.Label(stamRect, "STAM", _poiLabelStyle);

            // Bodkin Arrow Quiver Counter
            Rect quiverRect = new Rect(vitalsBox.x + 258f, vitalsBox.y + 8f, 128f, 38f);
            DrawMedievalPanel(quiverRect, ColIronPanel, ColParchmentMuted, 1f);
            GUI.Label(new Rect(quiverRect.x, quiverRect.y + 2f, quiverRect.width, 16f), "BODKIN ARROWS", _poiLabelStyle);
            GUI.Label(new Rect(quiverRect.x, quiverRect.y + 16f, quiverRect.width, 20f), $"x{actor.ArrowAmmo}", _monoBoldStyle);

            // 4-Slot Interactive Weapon & Alchemy Hotbar
            float slotW = 94f;
            float slotGap = 6.6f;
            float slotY = baseY + 62f;
            string[] slotHeaders = { "[1] MELEE", "[2] RANGED", "[3] TACTICAL", "[4] ALCHEMY" };

            for (int s = 0; s < 4; s++)
            {
                Rect sRect = new Rect(startX + s * (slotW + slotGap), slotY, slotW, 58f);
                bool active = actor.ActiveSlot == s;
                WeaponId wid = actor.Loadout[s];
                Color border = active ? ColBrassBorder : new Color(0.38f, 0.34f, 0.28f, 0.85f);
                DrawMedievalPanel(sRect, active ? new Color(0.22f, 0.18f, 0.12f, 0.95f) : ColIronDark, border, active ? 2.5f : 1f);

                string headerTxt = (s >= 2 && wid != WeaponId.None)
                    ? $"{slotHeaders[s]} x{actor.SlotCounts[s]}"
                    : slotHeaders[s];
                GUI.Label(new Rect(sRect.x, sRect.y + 3f, sRect.width, 15f), headerTxt, _poiLabelStyle);

                if (wid != WeaponId.None)
                {
                    WeaponSpec spec = MedievalCatalog.Get(wid);
                    Color rarCol = MedievalCatalog.GetRarityColor(spec.rarity);
                    DrawSolidRect(new Rect(sRect.x + 4f, sRect.y + 20f, sRect.width - 8f, 3f), rarCol);

                    GUI.Label(new Rect(sRect.x + 2f, sRect.y + 25f, sRect.width - 4f, 28f), spec.shortCode, _monoBoldStyle);
                }
                else
                {
                    GUI.Label(new Rect(sRect.x, sRect.y + 25f, sRect.width, 26f), "EMPTY", _poiLabelStyle);
                }

                // Tappable / Clickable on both PC and Mobile
                if (GUI.Button(sRect, GUIContent.none, GUIStyle.none) && actor.IsPlayer)
                {
                    actor.SelectSlot(s);
                }
            }
        }

        private void DrawNearbyLootPickupCard()
        {
            GroundLootItem item = _match.InputCamera.NearestPickupCandidate;
            if (item == null || item.collected || _match.PlayerActor == null || !_match.PlayerActor.IsAlive) return;

            WeaponSpec spec = MedievalCatalog.Get(item.weaponId);
            Color rarCol = MedievalCatalog.GetRarityColor(spec.rarity);

            float sw = Screen.width;
            float sh = Screen.height;
            Rect card = new Rect(sw * 0.5f + 85f, sh * 0.5f + 10f, 275f, 76f);
            DrawMedievalPanel(card, ColIronDark, rarCol, 2f);

            string stackStr = item.count > 1 ? $" (x{item.count})" : "";
            GUI.Label(new Rect(card.x + 10f, card.y + 6f, card.width - 20f, 22f), $"[E] EQUIP: {spec.displayName}{stackStr}", _headerStyle);
            GUI.Label(new Rect(card.x + 10f, card.y + 28f, card.width - 20f, 42f), spec.description, _bodyStyle);

            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                _match.InputCamera.MobilePickupPressed = true;
            }
        }

        private void DrawMobileTouchControlsOverlay()
        {
            CombatantActor player = _match.PlayerActor;
            if (player == null || !player.IsAlive) return;

            float sw = Screen.width;
            float sh = Screen.height;

            // 1. Left Virtual Thumbstick Pad
            Rect joyPadArea = new Rect(28f, sh - 245f, 175f, 175f);
            DrawMedievalPanel(joyPadArea, new Color(0.10f, 0.09f, 0.08f, 0.52f), ColBrassBorder, 1.5f);
            GUI.Label(new Rect(joyPadArea.x, joyPadArea.y + 6f, joyPadArea.width, 18f), "MOVE / OUTER = SPRINT", _poiLabelStyle);

            Vector2 defaultCenter = joyPadArea.center;
            Vector2 knobPos = _joyDragging ? _joyCurrent : defaultCenter;
            Rect knobRect = new Rect(knobPos.x - 26f, knobPos.y - 26f, 52f, 52f);
            DrawMedievalPanel(knobRect, ColCrimson, ColParchment, 2f);

            // Process Touch or Mouse Drag inside Left Joystick & Right Camera Look Area
            HandleVirtualTouchAndMouseInput(joyPadArea, sw, sh);

            // 2. Right-Hand Mobile Action Buttons
            float rightX = sw - 215f;
            float baseY = sh - 245f;

            // Primary ATTACK / FIRE Button
            Rect fireBtn = new Rect(rightX + 95f, baseY + 65f, 102f, 88f);
            if (DrawMedievalActionButton(fireBtn, "ATTACK /\nFIRE", ColCrimson))
            {
                _match.InputCamera.MobileFirePressed = true;
            }

            // AIM / SHIELD BLOCK Toggle Button
            Rect aimBtn = new Rect(rightX, baseY + 80f, 86f, 68f);
            string aimLabel = MedievalCatalog.Get(player.GetActiveWeaponId()).slotType == ItemSlotType.Melee ? "SHIELD\nBLOCK" : "AIM /\nDRAW";
            bool aimActive = _match.InputCamera.MobileAimToggle;
            if (DrawMedievalButton(aimBtn, aimLabel, aimActive))
            {
                _match.InputCamera.MobileAimToggle = !_match.InputCamera.MobileAimToggle;
                _match.InputCamera.MobileBlockHeld = _match.InputCamera.MobileAimToggle;
            }

            // JUMP / DIVE / GLIDER Button
            Rect jumpBtn = new Rect(rightX + 95f, baseY - 8f, 102f, 64f);
            string jumpLabel = player.CurrentDropState == DropState.AboardAirship
                ? "DIVE OFF!"
                : (player.CurrentDropState == DropState.Freefall ? "GLIDER!" : "JUMP");
            if (DrawMedievalActionButton(jumpBtn, jumpLabel, ColBrassBorder))
            {
                _match.InputCamera.MobileJumpPressed = true;
            }

            // QUICK HEAL POTION Button
            Rect healBtn = new Rect(rightX, baseY + 4f, 86f, 64f);
            int potCount = player.SlotCounts[3];
            if (DrawMedievalActionButton(healBtn, $"HEAL\n(x{potCount})", ColEmerald))
            {
                _match.InputCamera.MobileHealPressed = true;
            }
        }

        private void HandleVirtualTouchAndMouseInput(Rect joyPadArea, float sw, float sh)
        {
            Event ev = Event.current;
            if (ev == null) return;

            Vector2 guiPos = ev.mousePosition;
            Rect rightLookZone = new Rect(sw * 0.35f, 65f, sw * 0.45f, sh - 210f);

            if (ev.type == EventType.MouseDown)
            {
                if (joyPadArea.Contains(guiPos))
                {
                    _joyDragging = true;
                    _joyCenter = joyPadArea.center;
                    _joyCurrent = guiPos;
                    UpdateJoystickVector();
                    ev.Use();
                }
                else if (rightLookZone.Contains(guiPos))
                {
                    _lookDragging = true;
                    _lastLookPos = guiPos;
                    ev.Use();
                }
            }
            else if (ev.type == EventType.MouseDrag)
            {
                if (_joyDragging)
                {
                    Vector2 delta = guiPos - _joyCenter;
                    float maxR = 68f;
                    if (delta.magnitude > maxR) delta = delta.normalized * maxR;
                    _joyCurrent = _joyCenter + delta;
                    UpdateJoystickVector();
                    ev.Use();
                }
                else if (_lookDragging)
                {
                    Vector2 d = guiPos - _lastLookPos;
                    _lastLookPos = guiPos;
                    _match.InputCamera.MobileLookDelta += new Vector2(d.x, d.y);
                    ev.Use();
                }
            }
            else if (ev.type == EventType.MouseUp)
            {
                if (_joyDragging)
                {
                    _joyDragging = false;
                    _match.InputCamera.MobileMoveJoystick = Vector2.zero;
                    _match.InputCamera.MobileSprintActive = false;
                }
                if (_lookDragging)
                {
                    _lookDragging = false;
                }
            }
        }

        private void UpdateJoystickVector()
        {
            Vector2 delta = _joyCurrent - _joyCenter;
            float maxR = 68f;
            Vector2 norm = new Vector2(delta.x / maxR, -delta.y / maxR); // GUI Y is inverted
            _match.InputCamera.MobileMoveJoystick = Vector2.ClampMagnitude(norm, 1f);
            _match.InputCamera.MobileSprintActive = norm.magnitude > 0.78f;
        }

        private void DrawFullParchmentMapModal()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float size = Mathf.Min(sw - 80f, sh - 90f, 560f);
            Rect modal = new Rect((sw - size) * 0.5f, (sh - size) * 0.5f, size, size);

            DrawMedievalPanel(modal, ColIronDark, ColBrassBorder, 3f);
            GUI.Label(new Rect(modal.x, modal.y + 8f, modal.width, 26f), "TACTICAL PARCHMENT MAP OF HIGHKEEP ISLAND", _subtitleStyle);

            Rect mapArea = new Rect(modal.x + 16f, modal.y + 38f, modal.width - 32f, modal.height - 84f);
            if (_minimapTex != null)
            {
                GUI.DrawTexture(mapArea, _minimapTex);
            }

            // Overlay Landmark POI Names at their exact map coordinates
            float worldHalf = MedievalCatalog.IslandHalfExtent;
            for (int i = 0; i < MedievalCatalog.Landmarks.Length; i++)
            {
                LandmarkPOI poi = MedievalCatalog.Landmarks[i];
                float nx = (poi.worldXZ.x + worldHalf) / (worldHalf * 2f);
                float ny = 1f - ((poi.worldXZ.y + worldHalf) / (worldHalf * 2f)); // GUI Y is top-down
                float lx = mapArea.x + nx * mapArea.width;
                float ly = mapArea.y + ny * mapArea.height;

                Rect tagRect = new Rect(lx - 68f, ly - 11f, 136f, 22f);
                DrawMedievalPanel(tagRect, new Color(0.08f, 0.07f, 0.06f, 0.82f), ColBrassBorder, 1f);
                GUI.Label(tagRect, poi.name, _poiLabelStyle);
            }

            Rect closeBtn = new Rect(modal.center.x - 95f, modal.yMax - 40f, 190f, 32f);
            if (DrawMedievalActionButton(closeBtn, "CLOSE MAP [M]", ColCrimson))
            {
                _match.InputCamera.IsMapOpen = false;
            }
        }

        private void DrawWorldFloatingDamageNumbers()
        {
            Camera cam = _match.InputCamera.ActiveCamera;
            if (cam == null) return;

            List<FloatingDamageText> list = _match.FloatingDamages;
            float now = Time.time;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                FloatingDamageText fd = list[i];
                float age = now - fd.spawnTime;
                if (age > 1.15f)
                {
                    list.RemoveAt(i);
                    continue;
                }

                Vector3 wPos = fd.worldPos + Vector3.up * (age * 1.6f);
                Vector3 sp = cam.WorldToScreenPoint(wPos);
                if (sp.z <= 0f) continue;

                float guiY = Screen.height - sp.y;
                _damageNumberStyle.normal.textColor = fd.isHeadshot
                    ? ColCrimsonBright
                    : (fd.isArmorHit ? ColArmorBlue : ColBrassBorder);
                string prefix = fd.isHeadshot ? "CRIT " : "";
                GUI.Label(new Rect(sp.x - 60f, guiY - 14f, 120f, 28f), $"{prefix}-{fd.amount}", _damageNumberStyle);
            }
        }

        private void DrawEndOfMatchOrSpectateOverlay()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            bool won = _match.CurrentPhase == MatchPhase.Victory;

            Rect box = new Rect((sw - 500f) * 0.5f, 68f, 500f, 250f);
            DrawMedievalPanel(box, ColIronDark, won ? ColBrassBorder : ColCrimson, 2.5f);

            string headline = won ? "THE IRON CROWN IS YOURS!" : "FALLEN IN BATTLE";
            string subline = won
                ? "WINNER WINNER ROYAL PHEASANT DINNER — ALL RIVALS VANQUISHED"
                : $"FINAL PLACEMENT: #{_match.PlayerPlacement} / {(int)_match.SelectedLobbySize}";

            GUI.Label(new Rect(box.x, box.y + 16f, box.width, 36f), headline, _titleStyle);
            GUI.Label(new Rect(box.x, box.y + 52f, box.width, 24f), subline, _subtitleStyle);

            CombatantActor p = _match.PlayerActor;
            if (p != null)
            {
                string stats =
                    $"Eliminations: {p.Kills}     |     Total Damage Dealt: {Mathf.RoundToInt(p.TotalDamageDealt)}\n" +
                    $"Longest Ranged Hit: {Mathf.RoundToInt(p.LongestHitDistance)}m     |     Survivors Remaining: {_match.AliveCount}";
                GUI.Label(new Rect(box.x + 24f, box.y + 88f, box.width - 48f, 48f), stats, _monoBoldStyle);
            }

            float btnY = box.y + 148f;
            if (!won && _match.AliveCount > 0)
            {
                CombatantActor spec = _match.GetSpectateTarget();
                string specName = spec != null ? spec.DisplayName : "None";
                GUI.Label(new Rect(box.x, btnY - 6f, box.width, 22f), $"Spectating: {specName}", _subtitleStyle);

                if (DrawMedievalButton(new Rect(box.x + 28f, btnY + 22f, 140f, 42f), "SPECTATE NEXT", false))
                {
                    _match.CycleSpectateTarget();
                }
                if (DrawMedievalActionButton(new Rect(box.x + 180f, btnY + 22f, 145f, 42f), "PLAY AGAIN", ColCrimson))
                {
                    _match.StartNewMatch(_match.SelectedLobbySize, _match.SelectedDifficulty);
                }
                if (DrawMedievalButton(new Rect(box.x + 335f, btnY + 22f, 136f, 42f), "WAR COUNCIL", false))
                {
                    _match.ReturnToLobbyMenu();
                }
            }
            else
            {
                if (DrawMedievalActionButton(new Rect(box.x + 55f, btnY + 22f, 185f, 44f), "PLAY AGAIN", ColCrimson))
                {
                    _match.StartNewMatch(_match.SelectedLobbySize, _match.SelectedDifficulty);
                }
                if (DrawMedievalButton(new Rect(box.x + 260f, btnY + 22f, 185f, 44f), "CHANGE LOBBY SIZE", false))
                {
                    _match.ReturnToLobbyMenu();
                }
            }
        }

        // UI Drawing Helpers
        private void DrawMedievalPanel(Rect r, Color bg, Color border, float borderPx)
        {
            DrawSolidRect(r, bg);
            DrawSolidRect(new Rect(r.x, r.y, r.width, borderPx), border);
            DrawSolidRect(new Rect(r.x, r.yMax - borderPx, r.width, borderPx), border);
            DrawSolidRect(new Rect(r.x, r.y, borderPx, r.height), border);
            DrawSolidRect(new Rect(r.xMax - borderPx, r.y, borderPx, r.height), border);
        }

        private void DrawProgressBar(Rect r, float norm, Color fillCol)
        {
            DrawSolidRect(r, new Color(0.05f, 0.04f, 0.04f, 0.92f));
            float w = Mathf.Clamp01(norm) * (r.width - 2f);
            if (w > 0.5f)
            {
                DrawSolidRect(new Rect(r.x + 1f, r.y + 1f, w, r.height - 2f), fillCol);
            }
        }

        private bool DrawMedievalButton(Rect r, string label, bool selected)
        {
            Color bg = selected ? new Color(0.28f, 0.21f, 0.12f, 0.96f) : ColIronPanel;
            Color border = selected ? ColBrassBorder : new Color(0.42f, 0.37f, 0.30f, 0.85f);
            DrawMedievalPanel(r, bg, border, selected ? 2.5f : 1.2f);
            GUI.Label(r, label, _monoBoldStyle);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        private bool DrawMedievalActionButton(Rect r, string label, Color bgCol)
        {
            DrawMedievalPanel(r, bgCol, ColParchment, 2f);
            GUI.Label(r, label, _monoBoldStyle);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        private void DrawSolidRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _whiteTex);
            GUI.color = prev;
        }
    }
}
