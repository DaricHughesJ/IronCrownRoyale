using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public class GroundLootItem : MonoBehaviour
    {
        public WeaponId weaponId;
        public int count;
        public Transform visualRoot;
        public bool collected;
        private float _bobOffset;
        private Vector3 _basePos;

        public void Initialize(WeaponId id, int stackCount, Vector3 position)
        {
            weaponId = id;
            count = stackCount;
            collected = false;
            _basePos = position;
            transform.position = position;
            _bobOffset = Random.Range(0f, Mathf.PI * 2f);
            BuildVisual();
        }

        private void BuildVisual()
        {
            WeaponSpec spec = MedievalCatalog.Get(weaponId);
            Color rarityCol = MedievalCatalog.GetRarityColor(spec.rarity);

            GameObject root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;

            // Glowing rarity ring on ground
            Material ringMat = MaterialLibrary.GetEmissiveMaterial(rarityCol, rarityCol, 1.4f);
            MaterialLibrary.CreatePart("RarityPedestal", PrimitiveType.Cylinder, visualRoot,
                new Vector3(0f, 0.06f, 0f), new Vector3(1.05f, 0.04f, 1.05f), Quaternion.identity, ringMat, false);

            Material itemMat = MaterialLibrary.GetMaterial(spec.themeColor, 0.35f, 0.55f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.42f, 0.27f, 0.14f), 0.05f, 0.2f);
            Material steelMat = MaterialLibrary.GetMaterial(new Color(0.76f, 0.79f, 0.84f), 0.75f, 0.65f);

            switch (spec.slotType)
            {
                case ItemSlotType.Melee:
                    MaterialLibrary.CreatePart("Hilt", PrimitiveType.Cylinder, visualRoot,
                        new Vector3(0f, 0.55f, 0f), new Vector3(0.09f, 0.35f, 0.09f), Quaternion.Euler(0f, 0f, 28f), woodMat);
                    MaterialLibrary.CreatePart("Blade", PrimitiveType.Cube, visualRoot,
                        new Vector3(-0.18f, 0.88f, 0f), new Vector3(0.14f, 0.75f, 0.05f), Quaternion.Euler(0f, 0f, 28f),
                        spec.rarity == ItemRarity.Legendary ? ringMat : steelMat);
                    break;

                case ItemSlotType.Ranged:
                    MaterialLibrary.CreatePart("BowLimb", PrimitiveType.Cylinder, visualRoot,
                        new Vector3(0f, 0.68f, 0f), new Vector3(0.08f, 0.55f, 0.08f), Quaternion.Euler(0f, 0f, 40f), woodMat);
                    MaterialLibrary.CreatePart("CrossProd", PrimitiveType.Cube, visualRoot,
                        new Vector3(0f, 0.68f, 0f), new Vector3(0.65f, 0.08f, 0.12f), Quaternion.Euler(20f, 0f, 0f), itemMat);
                    break;

                case ItemSlotType.Throwable:
                    MaterialLibrary.CreatePart("FlaskOrAxe", PrimitiveType.Sphere, visualRoot,
                        new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.48f, 0.42f), Quaternion.identity, ringMat);
                    break;

                case ItemSlotType.Alchemy:
                    MaterialLibrary.CreatePart("PotionBottle", PrimitiveType.Cylinder, visualRoot,
                        new Vector3(0f, 0.55f, 0f), new Vector3(0.32f, 0.28f, 0.32f), Quaternion.identity, itemMat);
                    MaterialLibrary.CreatePart("Cork", PrimitiveType.Cylinder, visualRoot,
                        new Vector3(0f, 0.86f, 0f), new Vector3(0.14f, 0.08f, 0.14f), Quaternion.identity, woodMat);
                    break;

                case ItemSlotType.ArmorUpgrade:
                    MaterialLibrary.CreatePart("Cuirass", PrimitiveType.Cube, visualRoot,
                        new Vector3(0f, 0.65f, 0f), new Vector3(0.58f, 0.55f, 0.36f), Quaternion.identity, itemMat);
                    MaterialLibrary.CreatePart("PauldronL", PrimitiveType.Sphere, visualRoot,
                        new Vector3(-0.34f, 0.85f, 0f), new Vector3(0.28f, 0.24f, 0.28f), Quaternion.identity, steelMat);
                    MaterialLibrary.CreatePart("PauldronR", PrimitiveType.Sphere, visualRoot,
                        new Vector3(0.34f, 0.85f, 0f), new Vector3(0.28f, 0.24f, 0.28f), Quaternion.identity, steelMat);
                    break;

                default:
                    MaterialLibrary.CreatePart("Quiver", PrimitiveType.Cylinder, visualRoot,
                        new Vector3(0f, 0.55f, 0f), new Vector3(0.24f, 0.34f, 0.24f), Quaternion.Euler(0f, 0f, -18f), woodMat);
                    break;
            }
        }

        public void AnimateHover(float time)
        {
            if (collected || visualRoot == null) return;
            visualRoot.localPosition = new Vector3(0f, Mathf.Sin(time * 2.6f + _bobOffset) * 0.12f, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, (time * 55f + _bobOffset * 40f) % 360f, 0f);
        }
    }

    public class SupplyDropCrate : MonoBehaviour
    {
        public Vector3 targetGroundPos;
        public bool hasLanded;
        public bool opened;
        private Transform _canopy;
        private Transform _smokeColumn;

        public void Initialize(Vector3 groundPos)
        {
            targetGroundPos = groundPos;
            transform.position = groundPos + Vector3.up * 155f;
            hasLanded = false;
            opened = false;

            Material chestMat = MaterialLibrary.GetMaterial(new Color(0.55f, 0.16f, 0.16f), 0.2f, 0.4f);
            Material goldMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.92f, 0.72f, 0.22f), new Color(0.92f, 0.72f, 0.22f), 0.8f);
            Material canopyMat = MaterialLibrary.GetMaterial(new Color(0.88f, 0.78f, 0.56f), 0.05f, 0.2f);
            Material smokeMat = MaterialLibrary.GetTransparentMaterial(new Color(0.88f, 0.20f, 0.18f, 0.32f));

            MaterialLibrary.CreatePart("RoyalChest", PrimitiveType.Cube, transform,
                new Vector3(0f, 0.65f, 0f), new Vector3(1.6f, 1.2f, 1.1f), Quaternion.identity, chestMat, true);
            MaterialLibrary.CreatePart("GoldTrim", PrimitiveType.Cube, transform,
                new Vector3(0f, 0.95f, 0f), new Vector3(1.68f, 0.25f, 1.18f), Quaternion.identity, goldMat, false);

            GameObject canopyRoot = new GameObject("ParachuteCanopy");
            canopyRoot.transform.SetParent(transform, false);
            canopyRoot.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            _canopy = canopyRoot.transform;
            MaterialLibrary.CreatePart("Wing", PrimitiveType.Sphere, _canopy,
                Vector3.zero, new Vector3(4.5f, 1.6f, 4.5f), Quaternion.identity, canopyMat, false);

            GameObject smokeObj = MaterialLibrary.CreatePart("CrimsonBeacon", PrimitiveType.Cylinder, transform,
                new Vector3(0f, 24f, 0f), new Vector3(0.85f, 24f, 0.85f), Quaternion.identity, smokeMat, false);
            _smokeColumn = smokeObj.transform;
        }

        public void TickDrop(float dt, IslandWorldGenerator world)
        {
            if (opened) return;
            if (!hasLanded)
            {
                Vector3 p = transform.position;
                p.y -= 13.5f * dt;
                if (p.y <= targetGroundPos.y)
                {
                    p.y = targetGroundPos.y;
                    hasLanded = true;
                    if (_canopy != null) _canopy.gameObject.SetActive(false);
                    // Spawn guaranteed legendary/rare loot around the chest!
                    world.SpawnLootAt(targetGroundPos + new Vector3(1.8f, 0f, 0f), WeaponId.SunforgedGreatsword, 1);
                    world.SpawnLootAt(targetGroundPos + new Vector3(-1.8f, 0f, 0f), WeaponId.DwarvenRepeater, 1);
                    world.SpawnLootAt(targetGroundPos + new Vector3(0f, 0f, 1.8f), WeaponId.GildedPlateArmor, 1);
                    world.SpawnLootAt(targetGroundPos + new Vector3(0f, 0f, -1.8f), WeaponId.RoyalElixir, 2);
                }
                transform.position = p;
            }
            else if (_smokeColumn != null)
            {
                float pulse = 0.75f + Mathf.Sin(Time.time * 3.5f) * 0.22f;
                _smokeColumn.localScale = new Vector3(pulse, 24f, pulse);
            }
        }
    }

    [ExecuteAlways]
    public class IslandWorldGenerator : MonoBehaviour
    {
        public readonly List<GroundLootItem> ActiveLoot = new List<GroundLootItem>();
        public readonly List<SupplyDropCrate> SupplyDrops = new List<SupplyDropCrate>();
        public readonly List<Transform> AnimatedWindmills = new List<Transform>();

        private Transform _worldRoot;
        private Transform _lootRoot;
        private Mesh _terrainMesh;

        private void OnEnable()
        {
            if (!Application.isPlaying && _worldRoot == null)
            {
                GenerateWorld(true);
            }
        }

        public void GenerateWorld(bool isEditorPreview = false)
        {
            MedievalCatalog.EnsureInitialized();

            Transform oldChild = transform.Find("IslandWorldRoot");
            if (oldChild != null)
            {
                if (Application.isPlaying) Destroy(oldChild.gameObject);
                else DestroyImmediate(oldChild.gameObject);
            }
            if (_worldRoot != null)
            {
                if (Application.isPlaying) Destroy(_worldRoot.gameObject);
                else DestroyImmediate(_worldRoot.gameObject);
            }
            ActiveLoot.Clear();
            SupplyDrops.Clear();
            AnimatedWindmills.Clear();

            GameObject rootGo = new GameObject("IslandWorldRoot");
            if (isEditorPreview || !Application.isPlaying)
            {
                rootGo.hideFlags = HideFlags.DontSave;
            }
            _worldRoot = rootGo.transform;
            _worldRoot.SetParent(transform, false);

            _lootRoot = new GameObject("GroundLootRoot").transform;
            _lootRoot.SetParent(_worldRoot, false);

            BuildTerrainAndOcean();
            BuildAllLandmarks();
            BuildWildernessTreesAndRocks();
            if (!isEditorPreview && Application.isPlaying)
            {
                SpawnInitialIslandLoot();
            }
        }

        public static float SampleProceduralHeight(float x, float z)
        {
            float distFromCenter = Mathf.Sqrt(x * x + z * z);
            float maxR = MedievalCatalog.IslandHalfExtent;

            // Smooth island falloff near beaches and ocean
            float normalizedR = distFromCenter / maxR;
            float islandMask = Mathf.Clamp01(1.18f - normalizedR * normalizedR * 1.15f);

            // Multi-octave rolling medieval highlands
            float n1 = Mathf.PerlinNoise((x + 1200f) * 0.0032f, (z + 1200f) * 0.0032f);
            float n2 = Mathf.PerlinNoise((x + 400f) * 0.0095f, (z - 700f) * 0.0095f);
            float n3 = Mathf.PerlinNoise((x - 900f) * 0.024f, (z + 500f) * 0.024f);

            float rawHeight = (n1 * 28f + n2 * 9f + n3 * 2.5f) * islandMask;

            // Flatten plateaus around each Named Landmark so castles and cottages sit flush on solid ground
            for (int i = 0; i < MedievalCatalog.Landmarks.Length; i++)
            {
                LandmarkPOI poi = MedievalCatalog.Landmarks[i];
                float dx = x - poi.worldXZ.x;
                float dz = z - poi.worldXZ.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < poi.radius * 1.25f)
                {
                    float targetPlateau = poi.tier == 3 ? 25f : (poi.name.Contains("DOCKS") ? 6.8f : 14.5f);
                    float blend = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((d - poi.radius * 0.55f) / (poi.radius * 0.70f)));
                    rawHeight = Mathf.Lerp(rawHeight, targetPlateau, blend);
                }
            }

            return Mathf.Max(1.5f, rawHeight);
        }

        public float GetGroundHeight(float x, float z)
        {
            return SampleProceduralHeight(x, z);
        }

        private void BuildTerrainAndOcean()
        {
            int gridRes = 128;
            float totalSize = MedievalCatalog.IslandHalfExtent * 2.25f;
            float half = totalSize * 0.5f;

            Vector3[] verts = new Vector3[(gridRes + 1) * (gridRes + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            Color[] colors = new Color[verts.Length];
            int[] tris = new int[gridRes * gridRes * 6];

            Color sandCol = new Color(0.76f, 0.69f, 0.50f);
            Color grassLow = new Color(0.27f, 0.42f, 0.21f);
            Color grassHigh = new Color(0.35f, 0.47f, 0.25f);
            Color highlandRock = new Color(0.42f, 0.40f, 0.37f);

            int vIdx = 0;
            for (int z = 0; z <= gridRes; z++)
            {
                float fz = (float)z / gridRes;
                float wz = -half + fz * totalSize;
                for (int x = 0; x <= gridRes; x++)
                {
                    float fx = (float)x / gridRes;
                    float wx = -half + fx * totalSize;
                    float wy = SampleProceduralHeight(wx, wz);

                    verts[vIdx] = new Vector3(wx, wy, wz);
                    uvs[vIdx] = new Vector2(fx * 32f, fz * 32f);

                    if (wy < 6.4f)
                        colors[vIdx] = sandCol;
                    else if (wy < 16f)
                        colors[vIdx] = Color.Lerp(grassLow, grassHigh, (wy - 6.4f) / 9.6f);
                    else
                        colors[vIdx] = Color.Lerp(grassHigh, highlandRock, Mathf.Clamp01((wy - 16f) / 16f));

                    vIdx++;
                }
            }

            int tIdx = 0;
            for (int z = 0; z < gridRes; z++)
            {
                for (int x = 0; x < gridRes; x++)
                {
                    int i0 = z * (gridRes + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + (gridRes + 1);
                    int i3 = i2 + 1;

                    tris[tIdx++] = i0;
                    tris[tIdx++] = i2;
                    tris[tIdx++] = i1;

                    tris[tIdx++] = i1;
                    tris[tIdx++] = i2;
                    tris[tIdx++] = i3;
                }
            }

            _terrainMesh = new Mesh();
            _terrainMesh.name = "IronCrownIslandTerrain";
            _terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _terrainMesh.vertices = verts;
            _terrainMesh.uv = uvs;
            _terrainMesh.colors = colors;
            _terrainMesh.triangles = tris;
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();

            GameObject terrainGo = new GameObject("IslandTerrain");
            terrainGo.transform.SetParent(_worldRoot, false);
            MeshFilter mf = terrainGo.AddComponent<MeshFilter>();
            mf.sharedMesh = _terrainMesh;

            MeshRenderer mr = terrainGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.GetMaterial(new Color(0.32f, 0.45f, 0.24f), 0.0f, 0.12f);
            mr.receiveShadows = true;

            MeshCollider mc = terrainGo.AddComponent<MeshCollider>();
            mc.sharedMesh = _terrainMesh;

            // Surrounding deep medieval sea
            Material seaMat = MaterialLibrary.GetMaterial(new Color(0.11f, 0.25f, 0.36f), 0.35f, 0.72f);
            MaterialLibrary.CreatePart("SurroundingSea", PrimitiveType.Cube, _worldRoot,
                new Vector3(0f, MedievalCatalog.WaterLevel - 1.0f, 0f),
                new Vector3(3200f, 2.0f, 3200f),
                Quaternion.identity, seaMat, false);
        }

        private void BuildAllLandmarks()
        {
            // 1. HIGHKEEP CITADEL (Center-North Royal Fortress)
            BuildHighkeepCitadel(MedievalCatalog.Landmarks[0].worldXZ);

            // 2. OAKHAVEN HAMLET (Western Village & Windmill)
            BuildOakhavenHamlet(MedievalCatalog.Landmarks[1].worldXZ);

            // 3. IRONFORGE WATCH (Eastern Dwarven Garrison)
            BuildIronforgeWatch(MedievalCatalog.Landmarks[2].worldXZ);

            // 4. RAVENMIRE ABBEY (South-West Gothic Ruins)
            BuildRavenmireAbbey(MedievalCatalog.Landmarks[3].worldXZ);

            // 5. KINGSPORT DOCKS (Southern Harbor & Medieval Ships)
            BuildKingsportDocks(MedievalCatalog.Landmarks[4].worldXZ);

            // 6. SUNKEN STONES (North-West Elder Monolith Ring)
            BuildSunkenStones(MedievalCatalog.Landmarks[5].worldXZ);
        }

        private void BuildHighkeepCitadel(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_HighkeepCitadel").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material stoneMat = MaterialLibrary.GetMaterial(new Color(0.52f, 0.50f, 0.47f), 0.08f, 0.18f);
            Material darkStoneMat = MaterialLibrary.GetMaterial(new Color(0.36f, 0.35f, 0.33f), 0.10f, 0.20f);
            Material roofMat = MaterialLibrary.GetMaterial(new Color(0.58f, 0.18f, 0.18f), 0.1f, 0.25f);
            Material bannerMat = MaterialLibrary.GetMaterial(new Color(0.75f, 0.18f, 0.18f), 0.05f, 0.2f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.42f, 0.28f, 0.16f), 0.05f, 0.2f);

            // Stone courtyard paving
            MaterialLibrary.CreatePart("CourtyardPaving", PrimitiveType.Cube, root,
                new Vector3(0f, 0.1f, 0f), new Vector3(92f, 0.3f, 92f), Quaternion.identity, darkStoneMat, true);

            // 4 Corner Curtain Towers
            float wallExtent = 44f;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-wallExtent, 0f, -wallExtent),
                new Vector3(wallExtent, 0f, -wallExtent),
                new Vector3(-wallExtent, 0f, wallExtent),
                new Vector3(wallExtent, 0f, wallExtent)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                BuildWatchtower(root, corners[i], 14f, stoneMat, roofMat);
            }

            // Outer Curtain Walls with open gate passages on all 4 sides
            for (int side = 0; side < 4; side++)
            {
                Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
                Transform wallSide = new GameObject("CurtainWall_" + side).transform;
                wallSide.SetParent(root, false);
                wallSide.localRotation = rot;

                // Left & Right wall segments leaving an 11m central gateway
                MaterialLibrary.CreatePart("WallL", PrimitiveType.Cube, wallSide,
                    new Vector3(-24f, 4.5f, -wallExtent), new Vector3(32f, 9f, 3.2f), Quaternion.identity, stoneMat, true);
                MaterialLibrary.CreatePart("WallR", PrimitiveType.Cube, wallSide,
                    new Vector3(24f, 4.5f, -wallExtent), new Vector3(32f, 9f, 3.2f), Quaternion.identity, stoneMat, true);
                MaterialLibrary.CreatePart("GateArch", PrimitiveType.Cube, wallSide,
                    new Vector3(0f, 7.6f, -wallExtent), new Vector3(16f, 2.8f, 3.8f), Quaternion.identity, darkStoneMat, true);

                // Interior wooden ramp up to the ramparts
                MaterialLibrary.CreatePart("RampartStair", PrimitiveType.Cube, wallSide,
                    new Vector3(-22f, 4.2f, -wallExtent + 5.5f), new Vector3(4.5f, 0.6f, 12f), Quaternion.Euler(22f, 0f, 0f), woodMat, true);
            }

            // Central Great Keep (Enterable multi-door stronghold)
            BuildEnterableKeep(root, Vector3.zero, 28f, 16f, 28f, stoneMat, darkStoneMat, bannerMat);

            // Courtyard barracks & armory sheds
            BuildMedievalCottage(root, new Vector3(-24f, 0f, 16f), Quaternion.Euler(0f, 90f, 0f), stoneMat, roofMat, woodMat);
            BuildMedievalCottage(root, new Vector3(24f, 0f, -16f), Quaternion.Euler(0f, -90f, 0f), stoneMat, roofMat, woodMat);
        }

        private void BuildOakhavenHamlet(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_OakhavenHamlet").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material plasterMat = MaterialLibrary.GetMaterial(new Color(0.82f, 0.77f, 0.66f), 0.02f, 0.15f);
            Material thatchMat = MaterialLibrary.GetMaterial(new Color(0.68f, 0.54f, 0.26f), 0.05f, 0.15f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.40f, 0.26f, 0.14f), 0.05f, 0.2f);
            Material stoneMat = MaterialLibrary.GetMaterial(new Color(0.50f, 0.49f, 0.46f), 0.05f, 0.2f);

            // Ring of 6 enterable timber-framed cottages around the village green
            Vector3[] cottageOffsets = new Vector3[]
            {
                new Vector3(-28f, 0f, -18f),
                new Vector3(28f, 0f, -20f),
                new Vector3(-32f, 0f, 18f),
                new Vector3(30f, 0f, 22f),
                new Vector3(-6f, 0f, -36f),
                new Vector3(8f, 0f, 38f)
            };

            for (int i = 0; i < cottageOffsets.Length; i++)
            {
                Vector3 pos = cottageOffsets[i];
                pos.y = GetGroundHeight(centerXZ.x + pos.x, centerXZ.y + pos.z) - y;
                Quaternion lookCenter = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z).normalized, Vector3.up);
                BuildMedievalCottage(root, pos, lookCenter, plasterMat, thatchMat, woodMat);
            }

            // Central Windmill with rotating sails
            Transform mill = new GameObject("OakhavenWindmill").transform;
            mill.SetParent(root, false);
            mill.localPosition = new Vector3(0f, 0f, 0f);
            MaterialLibrary.CreatePart("MillBase", PrimitiveType.Cylinder, mill,
                new Vector3(0f, 4.5f, 0f), new Vector3(7.5f, 4.5f, 7.5f), Quaternion.identity, stoneMat, true);
            MaterialLibrary.CreatePart("MillTop", PrimitiveType.Cylinder, mill,
                new Vector3(0f, 11.5f, 0f), new Vector3(6.2f, 3.8f, 6.2f), Quaternion.identity, plasterMat, true);
            MaterialLibrary.CreatePart("MillCap", PrimitiveType.Sphere, mill,
                new Vector3(0f, 15.2f, 0f), new Vector3(6.6f, 3.2f, 6.6f), Quaternion.identity, woodMat, false);

            Transform sails = new GameObject("WindmillSails").transform;
            sails.SetParent(mill, false);
            sails.localPosition = new Vector3(0f, 12.8f, 3.4f);
            MaterialLibrary.CreatePart("SpokeV", PrimitiveType.Cube, sails, Vector3.zero, new Vector3(0.4f, 14f, 0.4f), Quaternion.identity, woodMat, false);
            MaterialLibrary.CreatePart("SpokeH", PrimitiveType.Cube, sails, Vector3.zero, new Vector3(14f, 0.4f, 0.4f), Quaternion.identity, woodMat, false);
            MaterialLibrary.CreatePart("CanvasV", PrimitiveType.Cube, sails, new Vector3(0.6f, 0f, 0.1f), new Vector3(1.4f, 12.8f, 0.08f), Quaternion.identity, thatchMat, false);
            MaterialLibrary.CreatePart("CanvasH", PrimitiveType.Cube, sails, new Vector3(0f, 0.6f, 0.1f), new Vector3(12.8f, 1.4f, 0.08f), Quaternion.identity, thatchMat, false);
            AnimatedWindmills.Add(sails);
        }

        private void BuildIronforgeWatch(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_IronforgeWatch").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material basaltMat = MaterialLibrary.GetMaterial(new Color(0.28f, 0.29f, 0.32f), 0.25f, 0.35f);
            Material bronzeMat = MaterialLibrary.GetMaterial(new Color(0.72f, 0.48f, 0.20f), 0.65f, 0.55f);
            Material emberMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.95f, 0.38f, 0.10f), new Color(0.95f, 0.35f, 0.08f), 1.8f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.38f, 0.24f, 0.14f), 0.05f, 0.2f);

            // 3 Dwarven Watchtowers & central Great Forge
            BuildWatchtower(root, new Vector3(-26f, 0f, -20f), 15f, basaltMat, bronzeMat);
            BuildWatchtower(root, new Vector3(26f, 0f, -20f), 15f, basaltMat, bronzeMat);
            BuildWatchtower(root, new Vector3(0f, 0f, 28f), 16f, basaltMat, bronzeMat);

            // Central Open Great Forge Pavilion
            BuildEnterableKeep(root, Vector3.zero, 20f, 10f, 18f, basaltMat, bronzeMat, emberMat);
            MaterialLibrary.CreatePart("ForgeCrucible", PrimitiveType.Cylinder, root,
                new Vector3(0f, 1.4f, 0f), new Vector3(3.2f, 1.4f, 3.2f), Quaternion.identity, emberMat, true);

            BuildMedievalCottage(root, new Vector3(-22f, 0f, 14f), Quaternion.Euler(0f, 120f, 0f), basaltMat, bronzeMat, woodMat);
            BuildMedievalCottage(root, new Vector3(22f, 0f, 14f), Quaternion.Euler(0f, -120f, 0f), basaltMat, bronzeMat, woodMat);
        }

        private void BuildRavenmireAbbey(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_RavenmireAbbey").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material ruinStone = MaterialLibrary.GetMaterial(new Color(0.42f, 0.45f, 0.43f), 0.05f, 0.15f);
            Material slateRoof = MaterialLibrary.GetMaterial(new Color(0.24f, 0.28f, 0.34f), 0.1f, 0.25f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.34f, 0.22f, 0.14f), 0.05f, 0.15f);

            // Gothic Nave Colonnade
            for (int i = -3; i <= 3; i++)
            {
                float zOff = i * 8f;
                MaterialLibrary.CreatePart("NavePillarL_" + i, PrimitiveType.Cube, root,
                    new Vector3(-9f, 5.5f, zOff), new Vector3(2.2f, 11f, 2.2f), Quaternion.identity, ruinStone, true);
                MaterialLibrary.CreatePart("NavePillarR_" + i, PrimitiveType.Cube, root,
                    new Vector3(9f, 5.5f, zOff), new Vector3(2.2f, 11f, 2.2f), Quaternion.identity, ruinStone, true);
                MaterialLibrary.CreatePart("NaveArch_" + i, PrimitiveType.Cube, root,
                    new Vector3(0f, 10.6f, zOff), new Vector3(20f, 1.6f, 1.8f), Quaternion.identity, slateRoof, true);
            }

            // Bell Tower at North End
            BuildWatchtower(root, new Vector3(0f, 0f, 32f), 19f, ruinStone, slateRoof);
            BuildMedievalCottage(root, new Vector3(-24f, 0f, -8f), Quaternion.Euler(0f, 90f, 0f), ruinStone, slateRoof, woodMat);
            BuildMedievalCottage(root, new Vector3(24f, 0f, 8f), Quaternion.Euler(0f, -90f, 0f), ruinStone, slateRoof, woodMat);
        }

        private void BuildKingsportDocks(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_KingsportDocks").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.45f, 0.30f, 0.16f), 0.05f, 0.2f);
            Material darkWood = MaterialLibrary.GetMaterial(new Color(0.32f, 0.20f, 0.11f), 0.05f, 0.2f);
            Material sailMat = MaterialLibrary.GetMaterial(new Color(0.88f, 0.83f, 0.72f), 0.02f, 0.15f);
            Material tileRoof = MaterialLibrary.GetMaterial(new Color(0.64f, 0.28f, 0.18f), 0.08f, 0.22f);
            Material stoneMat = MaterialLibrary.GetMaterial(new Color(0.55f, 0.53f, 0.49f), 0.05f, 0.2f);

            // Harbor warehouses on shore
            BuildMedievalCottage(root, new Vector3(-22f, 0f, 16f), Quaternion.Euler(0f, 180f, 0f), stoneMat, tileRoof, woodMat);
            BuildMedievalCottage(root, new Vector3(0f, 0f, 20f), Quaternion.Euler(0f, 180f, 0f), stoneMat, tileRoof, woodMat);
            BuildMedievalCottage(root, new Vector3(22f, 0f, 16f), Quaternion.Euler(0f, 180f, 0f), stoneMat, tileRoof, woodMat);

            // Long wooden piers extending south toward the water
            for (int p = -1; p <= 1; p += 2)
            {
                float px = p * 18f;
                MaterialLibrary.CreatePart("PierDeck_" + p, PrimitiveType.Cube, root,
                    new Vector3(px, 0.4f, -24f), new Vector3(6.5f, 0.6f, 44f), Quaternion.identity, woodMat, true);

                // Moored Medieval Cog Ship beside each pier
                Transform ship = new GameObject("MedievalCogShip_" + p).transform;
                ship.SetParent(root, false);
                ship.localPosition = new Vector3(px + p * 9.5f, -0.5f, -34f);
                MaterialLibrary.CreatePart("Hull", PrimitiveType.Cube, ship,
                    new Vector3(0f, 1.6f, 0f), new Vector3(7.5f, 3.6f, 22f), Quaternion.identity, darkWood, true);
                MaterialLibrary.CreatePart("SternCastle", PrimitiveType.Cube, ship,
                    new Vector3(0f, 4.4f, 7.5f), new Vector3(7.2f, 2.8f, 6.5f), Quaternion.identity, woodMat, true);
                MaterialLibrary.CreatePart("Mast", PrimitiveType.Cylinder, ship,
                    new Vector3(0f, 9.5f, -1f), new Vector3(0.6f, 8.5f, 0.6f), Quaternion.identity, darkWood, false);
                MaterialLibrary.CreatePart("SquareSail", PrimitiveType.Cube, ship,
                    new Vector3(0f, 10.5f, -1.5f), new Vector3(9.5f, 6.5f, 0.3f), Quaternion.identity, sailMat, false);
            }
        }

        private void BuildSunkenStones(Vector2 centerXZ)
        {
            float y = GetGroundHeight(centerXZ.x, centerXZ.y);
            Transform root = new GameObject("POI_SunkenStones").transform;
            root.SetParent(_worldRoot, false);
            root.position = new Vector3(centerXZ.x, y, centerXZ.y);

            Material monolithMat = MaterialLibrary.GetMaterial(new Color(0.46f, 0.48f, 0.46f), 0.08f, 0.22f);
            Material runeMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.25f, 0.72f, 0.85f), new Color(0.25f, 0.72f, 0.85f), 1.4f);

            int ringCount = 10;
            float ringRadius = 24f;
            for (int i = 0; i < ringCount; i++)
            {
                float ang = (i / (float)ringCount) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * ringRadius, 4.2f, Mathf.Sin(ang) * ringRadius);
                Quaternion rot = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z).normalized, Vector3.up);
                MaterialLibrary.CreatePart("StandingStone_" + i, PrimitiveType.Cube, root,
                    pos, new Vector3(3.2f, 8.4f, 1.8f), rot, monolithMat, true);
                if (i % 2 == 0)
                {
                    MaterialLibrary.CreatePart("Lintel_" + i, PrimitiveType.Cube, root,
                        pos + Vector3.up * 4.6f, new Vector3(6.8f, 1.4f, 2.2f), rot, monolithMat, true);
                }
            }

            // Central Altar Pedestal
            MaterialLibrary.CreatePart("RunicAltar", PrimitiveType.Cylinder, root,
                new Vector3(0f, 0.9f, 0f), new Vector3(5.5f, 0.9f, 5.5f), Quaternion.identity, monolithMat, true);
            MaterialLibrary.CreatePart("RuneCrystal", PrimitiveType.Sphere, root,
                new Vector3(0f, 2.6f, 0f), new Vector3(1.4f, 2.2f, 1.4f), Quaternion.identity, runeMat, false);
        }

        private void BuildEnterableKeep(Transform parent, Vector3 localPos, float width, float height, float depth, Material wallMat, Material trimMat, Material bannerMat)
        {
            Transform keep = new GameObject("GreatKeep").transform;
            keep.SetParent(parent, false);
            keep.localPosition = localPos;

            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float doorWidth = 5.4f;
            float segW = (width - doorWidth) * 0.5f;

            // Front & Back Walls with open central doorways
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * hd;
                MaterialLibrary.CreatePart("KeepWallL_" + side, PrimitiveType.Cube, keep,
                    new Vector3(-(doorWidth * 0.5f + segW * 0.5f), height * 0.5f, z),
                    new Vector3(segW, height, 1.6f), Quaternion.identity, wallMat, true);
                MaterialLibrary.CreatePart("KeepWallR_" + side, PrimitiveType.Cube, keep,
                    new Vector3(doorWidth * 0.5f + segW * 0.5f, height * 0.5f, z),
                    new Vector3(segW, height, 1.6f), Quaternion.identity, wallMat, true);
                MaterialLibrary.CreatePart("KeepLintel_" + side, PrimitiveType.Cube, keep,
                    new Vector3(0f, height - 2.0f, z),
                    new Vector3(doorWidth + 0.4f, 4.0f, 1.8f), Quaternion.identity, trimMat, true);
            }

            // Left & Right Walls with open side doorways
            float segD = (depth - doorWidth) * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * hw;
                MaterialLibrary.CreatePart("KeepSideA_" + side, PrimitiveType.Cube, keep,
                    new Vector3(x, height * 0.5f, -(doorWidth * 0.5f + segD * 0.5f)),
                    new Vector3(1.6f, height, segD), Quaternion.identity, wallMat, true);
                MaterialLibrary.CreatePart("KeepSideB_" + side, PrimitiveType.Cube, keep,
                    new Vector3(x, height * 0.5f, doorWidth * 0.5f + segD * 0.5f),
                    new Vector3(1.6f, height, segD), Quaternion.identity, wallMat, true);
                MaterialLibrary.CreatePart("KeepSideLintel_" + side, PrimitiveType.Cube, keep,
                    new Vector3(x, height - 2.0f, 0f),
                    new Vector3(1.8f, 4.0f, doorWidth + 0.4f), Quaternion.identity, trimMat, true);
            }

            // Roof Deck & Heraldic Banners
            MaterialLibrary.CreatePart("KeepRoof", PrimitiveType.Cube, keep,
                new Vector3(0f, height + 0.5f, 0f), new Vector3(width + 1.8f, 1.0f, depth + 1.8f), Quaternion.identity, trimMat, true);
            MaterialLibrary.CreatePart("BannerFront", PrimitiveType.Cube, keep,
                new Vector3(0f, height * 0.65f, -hd - 0.95f), new Vector3(2.4f, 6.5f, 0.15f), Quaternion.identity, bannerMat, false);
            MaterialLibrary.CreatePart("BannerBack", PrimitiveType.Cube, keep,
                new Vector3(0f, height * 0.65f, hd + 0.95f), new Vector3(2.4f, 6.5f, 0.15f), Quaternion.identity, bannerMat, false);
        }

        private void BuildMedievalCottage(Transform parent, Vector3 localPos, Quaternion localRot, Material wallMat, Material roofMat, Material beamMat)
        {
            Transform house = new GameObject("MedievalCottage").transform;
            house.SetParent(parent, false);
            house.localPosition = localPos;
            house.localRotation = localRot;

            float w = 9.5f;
            float d = 11.5f;
            float h = 4.6f;

            // Floor slab
            MaterialLibrary.CreatePart("Floor", PrimitiveType.Cube, house,
                new Vector3(0f, 0.15f, 0f), new Vector3(w, 0.3f, d), Quaternion.identity, beamMat, true);

            // Back, Left, Right walls
            MaterialLibrary.CreatePart("BackWall", PrimitiveType.Cube, house,
                new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.55f), Quaternion.identity, wallMat, true);
            MaterialLibrary.CreatePart("LeftWall", PrimitiveType.Cube, house,
                new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.55f, h, d), Quaternion.identity, wallMat, true);
            MaterialLibrary.CreatePart("RightWall", PrimitiveType.Cube, house,
                new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.55f, h, d), Quaternion.identity, wallMat, true);

            // Front wall with open 3.2m doorway
            MaterialLibrary.CreatePart("FrontWallL", PrimitiveType.Cube, house,
                new Vector3(-3.15f, h * 0.5f, d * 0.5f), new Vector3(3.2f, h, 0.55f), Quaternion.identity, wallMat, true);
            MaterialLibrary.CreatePart("FrontWallR", PrimitiveType.Cube, house,
                new Vector3(3.15f, h * 0.5f, d * 0.5f), new Vector3(3.2f, h, 0.55f), Quaternion.identity, wallMat, true);
            MaterialLibrary.CreatePart("DoorHeader", PrimitiveType.Cube, house,
                new Vector3(0f, h - 0.65f, d * 0.5f), new Vector3(3.3f, 1.3f, 0.65f), Quaternion.identity, beamMat, true);

            // Pitched Roof Slopes
            MaterialLibrary.CreatePart("RoofSlopeL", PrimitiveType.Cube, house,
                new Vector3(-2.6f, h + 1.45f, 0f), new Vector3(6.2f, 0.45f, d + 1.4f), Quaternion.Euler(0f, 0f, 28f), roofMat, true);
            MaterialLibrary.CreatePart("RoofSlopeR", PrimitiveType.Cube, house,
                new Vector3(2.6f, h + 1.45f, 0f), new Vector3(6.2f, 0.45f, d + 1.4f), Quaternion.Euler(0f, 0f, -28f), roofMat, true);
        }

        private void BuildWatchtower(Transform parent, Vector3 localPos, float height, Material stoneMat, Material roofMat)
        {
            Transform tower = new GameObject("Watchtower").transform;
            tower.SetParent(parent, false);
            tower.localPosition = localPos;

            MaterialLibrary.CreatePart("TowerShaft", PrimitiveType.Cylinder, tower,
                new Vector3(0f, height * 0.5f, 0f), new Vector3(6.8f, height * 0.5f, 6.8f), Quaternion.identity, stoneMat, true);
            MaterialLibrary.CreatePart("BattlementDeck", PrimitiveType.Cylinder, tower,
                new Vector3(0f, height + 0.5f, 0f), new Vector3(8.0f, 0.6f, 8.0f), Quaternion.identity, roofMat, true);
        }

        private void BuildWildernessTreesAndRocks()
        {
            Transform natureRoot = new GameObject("WildernessTreesAndRocks").transform;
            natureRoot.SetParent(_worldRoot, false);

            Material trunkMat = MaterialLibrary.GetMaterial(new Color(0.36f, 0.23f, 0.12f), 0.02f, 0.1f);
            Material pineMatA = MaterialLibrary.GetMaterial(new Color(0.16f, 0.34f, 0.16f), 0.02f, 0.12f);
            Material pineMatB = MaterialLibrary.GetMaterial(new Color(0.22f, 0.40f, 0.18f), 0.02f, 0.12f);
            Material rockMat = MaterialLibrary.GetMaterial(new Color(0.45f, 0.44f, 0.42f), 0.06f, 0.18f);

            Random.State prevState = Random.state;
            Random.InitState(1415); // Deterministic Agincourt seed for consistent terrain cover

            int placedTrees = 0;
            int attempts = 0;
            while (placedTrees < 340 && attempts < 1400)
            {
                attempts++;
                float x = Random.Range(-500f, 500f);
                float z = Random.Range(-500f, 500f);
                if (x * x + z * z > 510f * 510f) continue;

                // Keep landmark courtyards clear of random trees
                bool insidePOI = false;
                for (int i = 0; i < MedievalCatalog.Landmarks.Length; i++)
                {
                    if (Vector2.Distance(new Vector2(x, z), MedievalCatalog.Landmarks[i].worldXZ) < MedievalCatalog.Landmarks[i].radius * 0.72f)
                    {
                        insidePOI = true;
                        break;
                    }
                }
                if (insidePOI) continue;

                float y = GetGroundHeight(x, z);
                if (y < 7.2f) continue; // Above beach line

                float scale = Random.Range(0.85f, 1.45f);
                Transform tree = new GameObject("Tree_" + placedTrees).transform;
                tree.SetParent(natureRoot, false);
                tree.position = new Vector3(x, y, z);
                tree.localScale = Vector3.one * scale;

                MaterialLibrary.CreatePart("Trunk", PrimitiveType.Cylinder, tree,
                    new Vector3(0f, 2.2f, 0f), new Vector3(0.75f, 2.2f, 0.75f), Quaternion.identity, trunkMat, true);
                Material foliage = (placedTrees % 2 == 0) ? pineMatA : pineMatB;
                MaterialLibrary.CreatePart("CanopyLow", PrimitiveType.Sphere, tree,
                    new Vector3(0f, 5.2f, 0f), new Vector3(4.2f, 3.6f, 4.2f), Quaternion.identity, foliage, false);
                MaterialLibrary.CreatePart("CanopyHigh", PrimitiveType.Sphere, tree,
                    new Vector3(0f, 7.6f, 0f), new Vector3(2.8f, 2.8f, 2.8f), Quaternion.identity, foliage, false);
                placedTrees++;
            }

            // Scatter 110 cover boulders across hillsides
            for (int r = 0; r < 110; r++)
            {
                float x = Random.Range(-480f, 480f);
                float z = Random.Range(-480f, 480f);
                float y = GetGroundHeight(x, z);
                if (y < 6.0f) continue;

                Vector3 scale = new Vector3(Random.Range(2.2f, 4.8f), Random.Range(1.6f, 3.4f), Random.Range(2.2f, 4.8f));
                MaterialLibrary.CreatePart("Boulder_" + r, PrimitiveType.Sphere, natureRoot,
                    new Vector3(x, y + scale.y * 0.35f, z), scale,
                    Quaternion.Euler(Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f)),
                    rockMat, true);
            }

            Random.state = prevState;
        }

        private void SpawnInitialIslandLoot()
        {
            WeaponId[] poiLootPool = new WeaponId[]
            {
                WeaponId.ArmingSword, WeaponId.RoyalHalberd, WeaponId.SunforgedGreatsword,
                WeaponId.HuntingShortbow, WeaponId.WarLongbow, WeaponId.HeavyArbalest, WeaponId.DwarvenRepeater,
                WeaponId.FranciscaAxes, WeaponId.AlchemicalFireFlask,
                WeaponId.LinenBandage, WeaponId.ApothecaryVial, WeaponId.RoyalElixir,
                WeaponId.BoiledLeatherArmor, WeaponId.ChainmailHauberk, WeaponId.GildedPlateArmor,
                WeaponId.BodkinArrowQuiver
            };

            // Dense loot inside and around all 6 Named Landmarks
            for (int i = 0; i < MedievalCatalog.Landmarks.Length; i++)
            {
                LandmarkPOI poi = MedievalCatalog.Landmarks[i];
                int lootCount = poi.tier == 3 ? 44 : 26;
                for (int j = 0; j < lootCount; j++)
                {
                    Vector2 offset = Random.insideUnitCircle * (poi.radius * 0.68f);
                    float wx = poi.worldXZ.x + offset.x;
                    float wz = poi.worldXZ.y + offset.y;
                    float wy = GetGroundHeight(wx, wz);

                    WeaponId chosen = PickWeightedLoot(poi.tier, poiLootPool);
                    int count = GetDefaultStackCount(chosen);
                    SpawnLootAt(new Vector3(wx, wy, wz), chosen, count);
                }
            }

            // Additional 95 wilderness loot caches across the island so players/bots landing anywhere find gear
            for (int k = 0; k < 95; k++)
            {
                Vector2 rnd = Random.insideUnitCircle * 450f;
                float wy = GetGroundHeight(rnd.x, rnd.y);
                if (wy < 6.8f) continue;

                WeaponId chosen = PickWeightedLoot(1, poiLootPool);
                SpawnLootAt(new Vector3(rnd.x, wy, rnd.y), chosen, GetDefaultStackCount(chosen));
            }
        }

        private WeaponId PickWeightedLoot(int poiTier, WeaponId[] pool)
        {
            float roll = Random.value;
            if (roll < 0.16f) return WeaponId.BodkinArrowQuiver;
            if (roll < 0.30f) return Random.value < 0.6f ? WeaponId.LinenBandage : WeaponId.ApothecaryVial;
            if (roll < 0.46f)
            {
                if (poiTier == 3 && Random.value < 0.30f) return WeaponId.GildedPlateArmor;
                return Random.value < 0.55f ? WeaponId.BoiledLeatherArmor : WeaponId.ChainmailHauberk;
            }
            if (roll < 0.68f)
            {
                if (poiTier == 3 && Random.value < 0.25f) return WeaponId.DwarvenRepeater;
                float r2 = Random.value;
                if (r2 < 0.45f) return WeaponId.HuntingShortbow;
                if (r2 < 0.75f) return WeaponId.WarLongbow;
                return WeaponId.HeavyArbalest;
            }
            if (roll < 0.86f)
            {
                if (poiTier == 3 && Random.value < 0.25f) return WeaponId.SunforgedGreatsword;
                return Random.value < 0.60f ? WeaponId.ArmingSword : WeaponId.RoyalHalberd;
            }
            return Random.value < 0.55f ? WeaponId.FranciscaAxes : WeaponId.AlchemicalFireFlask;
        }

        private int GetDefaultStackCount(WeaponId id)
        {
            WeaponSpec spec = MedievalCatalog.Get(id);
            return Mathf.Max(1, spec.defaultCount);
        }

        public GroundLootItem SpawnLootAt(Vector3 worldPos, WeaponId id, int count)
        {
            GameObject go = new GameObject("Loot_" + id);
            go.transform.SetParent(_lootRoot, false);
            GroundLootItem item = go.AddComponent<GroundLootItem>();
            item.Initialize(id, count, worldPos);
            ActiveLoot.Add(item);
            return item;
        }

        public SupplyDropCrate SpawnRoyalSupplyDrop(Vector2 safeCenterXZ, float safeRadius)
        {
            Vector2 rnd = Random.insideUnitCircle * Mathf.Min(safeRadius * 0.65f, 340f);
            float wx = Mathf.Clamp(safeCenterXZ.x + rnd.x, -440f, 440f);
            float wz = Mathf.Clamp(safeCenterXZ.y + rnd.y, -440f, 440f);
            float wy = GetGroundHeight(wx, wz);

            GameObject dropGo = new GameObject("RoyalGriffinSupplyDrop");
            dropGo.transform.SetParent(_worldRoot, false);
            SupplyDropCrate crate = dropGo.AddComponent<SupplyDropCrate>();
            crate.Initialize(new Vector3(wx, wy, wz));
            SupplyDrops.Add(crate);
            return crate;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            float t = Time.time;
            float dt = Time.deltaTime;

            for (int i = 0; i < AnimatedWindmills.Count; i++)
            {
                if (AnimatedWindmills[i] != null)
                {
                    AnimatedWindmills[i].Rotate(0f, 0f, 28f * dt, Space.Self);
                }
            }

            for (int i = 0; i < ActiveLoot.Count; i++)
            {
                GroundLootItem item = ActiveLoot[i];
                if (item != null && !item.collected)
                {
                    item.AnimateHover(t);
                }
            }

            for (int i = 0; i < SupplyDrops.Count; i++)
            {
                if (SupplyDrops[i] != null)
                {
                    SupplyDrops[i].TickDrop(dt, this);
                }
            }
        }
    }
}
