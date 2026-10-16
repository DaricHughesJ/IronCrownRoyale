using UnityEngine;

namespace IronCrown
{
    public class CombatantActor : MonoBehaviour
    {
        public int ActorId;
        public string DisplayName;
        public bool IsPlayer;
        public bool IsAlive = true;
        public Color HeraldicColor;

        public DropState CurrentDropState = DropState.AboardAirship;
        public float PlannedJumpProgress; // Used by bots to jump along the airship route

        // Vitals & Armor
        public float Health = 100f;
        public float MaxHealth = 100f;
        public int ArmorTier = 0; // 0 = Tunic, 1 = Leather, 2 = Chainmail, 3 = Plate
        public float ArmorPoints = 0f;
        public float MaxArmorPoints = 0f;
        public float Stamina = 100f;
        public float MaxStamina = 100f;

        // 4-Slot Loadout (0: Melee, 1: Ranged, 2: Throwable, 3: Alchemy)
        public WeaponId[] Loadout = new WeaponId[4];
        public int[] SlotCounts = new int[4];
        public int ActiveSlot = 0;
        public int ArrowAmmo = 16;

        // Combat & Movement State
        public bool IsBlocking;
        public bool IsAimingOrDrawing;
        public float BowDrawCharge; // 0..1
        public float AimPitchDegrees;
        public int Kills;
        public float TotalDamageDealt;
        public float LongestHitDistance;

        private CharacterController _cc;
        private float _verticalVelocity;
        private float _attackCooldownTimer;
        private float _swingAnimTimer;
        private float _walkCycle;
        private float _zoneTickTimer;

        // Articulated 3D Rig Transforms
        public Transform VisualRoot { get; private set; }
        public Transform HeadTransform { get; private set; }
        private Transform _torso;
        private Transform _pauldronL;
        private Transform _pauldronR;
        private Transform _helmet;
        private Transform _plume;
        private Transform _armL;
        private Transform _armR;
        private Transform _legL;
        private Transform _legR;
        private Transform _shieldMesh;
        private Transform _weaponHandRoot;
        private Transform _gliderRoot;

        public void InitializeActor(int id, string actorName, bool isPlayer, Color bannerColor)
        {
            ActorId = id;
            DisplayName = actorName;
            IsPlayer = isPlayer;
            HeraldicColor = bannerColor;
            IsAlive = true;

            Health = 100f;
            MaxHealth = 100f;
            ArmorTier = 0;
            ArmorPoints = 0f;
            MaxArmorPoints = 0f;
            Stamina = 100f;
            MaxStamina = 100f;
            Kills = 0;
            TotalDamageDealt = 0f;
            LongestHitDistance = 0f;

            // Starter Loadout: Oaken Cudgel + 1 Linen Poultice + 12 Arrows
            Loadout[0] = WeaponId.OakenClub;
            SlotCounts[0] = 1;
            Loadout[1] = WeaponId.None;
            SlotCounts[1] = 0;
            Loadout[2] = WeaponId.None;
            SlotCounts[2] = 0;
            Loadout[3] = WeaponId.LinenBandage;
            SlotCounts[3] = 1;
            ActiveSlot = 0;
            ArrowAmmo = 12;

            CurrentDropState = DropState.AboardAirship;

            _cc = gameObject.GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();
            }
            _cc.height = 1.9f;
            _cc.radius = 0.42f;
            _cc.center = new Vector3(0f, 0.95f, 0f);
            _cc.slopeLimit = 52f;
            _cc.stepOffset = 0.45f;
            _cc.enabled = false;

            BuildArticulatedKnightMesh();
            RefreshArmorVisuals();
            RefreshHeldWeaponVisual();
        }

        private void BuildArticulatedKnightMesh()
        {
            if (VisualRoot != null)
            {
                Destroy(VisualRoot.gameObject);
            }

            VisualRoot = new GameObject("KnightVisualRoot").transform;
            VisualRoot.SetParent(transform, false);

            Material skinMat = MaterialLibrary.GetMaterial(new Color(0.84f, 0.69f, 0.56f), 0.02f, 0.15f);
            Material bannerMat = MaterialLibrary.GetMaterial(HeraldicColor, 0.08f, 0.25f);
            Material bootMat = MaterialLibrary.GetMaterial(new Color(0.24f, 0.17f, 0.11f), 0.05f, 0.20f);
            Material steelMat = MaterialLibrary.GetMaterial(new Color(0.72f, 0.75f, 0.80f), 0.75f, 0.65f);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.44f, 0.29f, 0.16f), 0.05f, 0.20f);
            Material canvasMat = MaterialLibrary.GetMaterial(new Color(0.88f, 0.80f, 0.64f), 0.05f, 0.18f);

            // Torso & Heraldic Surcoat
            _torso = MaterialLibrary.CreatePart("Torso", PrimitiveType.Cube, VisualRoot,
                new Vector3(0f, 1.12f, 0f), new Vector3(0.56f, 0.66f, 0.34f), Quaternion.identity, bannerMat, false).transform;
            MaterialLibrary.CreatePart("SurcoatTabard", PrimitiveType.Cube, VisualRoot,
                new Vector3(0f, 0.92f, 0.01f), new Vector3(0.42f, 0.76f, 0.36f), Quaternion.identity, bannerMat, false);

            // Pauldrons (upgraded with armor tier)
            _pauldronL = MaterialLibrary.CreatePart("PauldronL", PrimitiveType.Sphere, VisualRoot,
                new Vector3(-0.36f, 1.38f, 0f), new Vector3(0.26f, 0.22f, 0.26f), Quaternion.identity, steelMat, false).transform;
            _pauldronR = MaterialLibrary.CreatePart("PauldronR", PrimitiveType.Sphere, VisualRoot,
                new Vector3(0.36f, 1.38f, 0f), new Vector3(0.26f, 0.22f, 0.26f), Quaternion.identity, steelMat, false).transform;

            // Head & Helmet
            HeadTransform = MaterialLibrary.CreatePart("Head", PrimitiveType.Sphere, VisualRoot,
                new Vector3(0f, 1.66f, 0f), new Vector3(0.36f, 0.38f, 0.36f), Quaternion.identity, skinMat, false).transform;
            _helmet = MaterialLibrary.CreatePart("Helmet", PrimitiveType.Cylinder, HeadTransform,
                new Vector3(0f, 0.12f, 0f), new Vector3(1.06f, 0.55f, 1.06f), Quaternion.identity, steelMat, false).transform;
            _plume = MaterialLibrary.CreatePart("HeraldicPlume", PrimitiveType.Cube, HeadTransform,
                new Vector3(0f, 0.72f, -0.15f), new Vector3(0.18f, 0.48f, 0.65f), Quaternion.Euler(-18f, 0f, 0f), bannerMat, false).transform;

            // Left Arm + Heater Shield
            _armL = new GameObject("LeftArmPivot").transform;
            _armL.SetParent(VisualRoot, false);
            _armL.localPosition = new Vector3(-0.38f, 1.36f, 0f);
            MaterialLibrary.CreatePart("ArmLMesh", PrimitiveType.Cube, _armL,
                new Vector3(0f, -0.26f, 0f), new Vector3(0.18f, 0.54f, 0.18f), Quaternion.identity, steelMat, false);

            _shieldMesh = new GameObject("HeaterShield").transform;
            _shieldMesh.SetParent(_armL, false);
            _shieldMesh.localPosition = new Vector3(-0.14f, -0.30f, 0.12f);
            MaterialLibrary.CreatePart("ShieldBoard", PrimitiveType.Cube, _shieldMesh,
                Vector3.zero, new Vector3(0.08f, 0.68f, 0.52f), Quaternion.identity, bannerMat, false);
            MaterialLibrary.CreatePart("ShieldBoss", PrimitiveType.Sphere, _shieldMesh,
                new Vector3(-0.04f, 0f, 0f), new Vector3(0.10f, 0.22f, 0.22f), Quaternion.identity, steelMat, false);

            // Right Arm + Held Weapon Root
            _armR = new GameObject("RightArmPivot").transform;
            _armR.SetParent(VisualRoot, false);
            _armR.localPosition = new Vector3(0.38f, 1.36f, 0f);
            MaterialLibrary.CreatePart("ArmRMesh", PrimitiveType.Cube, _armR,
                new Vector3(0f, -0.26f, 0f), new Vector3(0.18f, 0.54f, 0.18f), Quaternion.identity, steelMat, false);

            _weaponHandRoot = new GameObject("WeaponHandRoot").transform;
            _weaponHandRoot.SetParent(_armR, false);
            _weaponHandRoot.localPosition = new Vector3(0.04f, -0.48f, 0.16f);

            // Legs
            _legL = new GameObject("LeftLegPivot").transform;
            _legL.SetParent(VisualRoot, false);
            _legL.localPosition = new Vector3(-0.16f, 0.76f, 0f);
            MaterialLibrary.CreatePart("LegLMesh", PrimitiveType.Cube, _legL,
                new Vector3(0f, -0.38f, 0f), new Vector3(0.21f, 0.74f, 0.21f), Quaternion.identity, bootMat, false);

            _legR = new GameObject("RightLegPivot").transform;
            _legR.SetParent(VisualRoot, false);
            _legR.localPosition = new Vector3(0.16f, 0.76f, 0f);
            MaterialLibrary.CreatePart("LegRMesh", PrimitiveType.Cube, _legR,
                new Vector3(0f, -0.38f, 0f), new Vector3(0.21f, 0.74f, 0.21f), Quaternion.identity, bootMat, false);

            // Overhead Medieval Canvas Glider (Active during DropState.Gliding)
            _gliderRoot = new GameObject("MedievalGliderRig").transform;
            _gliderRoot.SetParent(VisualRoot, false);
            _gliderRoot.localPosition = new Vector3(0f, 2.35f, 0f);
            MaterialLibrary.CreatePart("GliderSpar", PrimitiveType.Cylinder, _gliderRoot,
                Vector3.zero, new Vector3(0.10f, 2.4f, 0.10f), Quaternion.Euler(0f, 0f, 90f), woodMat, false);
            MaterialLibrary.CreatePart("WingL", PrimitiveType.Cube, _gliderRoot,
                new Vector3(-1.35f, 0.1f, -0.15f), new Vector3(2.5f, 0.08f, 1.55f), Quaternion.Euler(8f, -12f, -10f), canvasMat, false);
            MaterialLibrary.CreatePart("WingR", PrimitiveType.Cube, _gliderRoot,
                new Vector3(1.35f, 0.1f, -0.15f), new Vector3(2.5f, 0.08f, 1.55f), Quaternion.Euler(8f, 12f, 10f), canvasMat, false);
            MaterialLibrary.CreatePart("WingCrestL", PrimitiveType.Cube, _gliderRoot,
                new Vector3(-1.35f, 0.15f, -0.15f), new Vector3(0.75f, 0.09f, 1.4f), Quaternion.Euler(8f, -12f, -10f), bannerMat, false);
            MaterialLibrary.CreatePart("WingCrestR", PrimitiveType.Cube, _gliderRoot,
                new Vector3(1.35f, 0.15f, -0.15f), new Vector3(0.75f, 0.09f, 1.4f), Quaternion.Euler(8f, 12f, 10f), bannerMat, false);
            _gliderRoot.gameObject.SetActive(false);
        }

        public void RefreshArmorVisuals()
        {
            if (_torso == null) return;
            Color armorCol;
            float metallic = 0.1f;
            float smooth = 0.25f;

            switch (ArmorTier)
            {
                case 1: // Boiled Leather
                    armorCol = new Color(0.48f, 0.32f, 0.18f);
                    metallic = 0.15f;
                    smooth = 0.35f;
                    break;
                case 2: // Riveted Chainmail
                    armorCol = new Color(0.62f, 0.66f, 0.72f);
                    metallic = 0.72f;
                    smooth = 0.58f;
                    break;
                case 3: // Gilded Full Plate
                    armorCol = new Color(0.88f, 0.72f, 0.28f);
                    metallic = 0.85f;
                    smooth = 0.75f;
                    break;
                default: // Peasant Tunic
                    armorCol = new Color(0.36f, 0.32f, 0.26f);
                    break;
            }

            Material mat = MaterialLibrary.GetMaterial(armorCol, metallic, smooth);
            _torso.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _pauldronL.gameObject.SetActive(ArmorTier >= 1);
            _pauldronR.gameObject.SetActive(ArmorTier >= 1);
            _pauldronL.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _pauldronR.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _helmet.gameObject.SetActive(ArmorTier >= 1);
            _helmet.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _plume.gameObject.SetActive(ArmorTier >= 2);
        }

        public void RefreshHeldWeaponVisual()
        {
            if (_weaponHandRoot == null) return;
            for (int i = _weaponHandRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_weaponHandRoot.GetChild(i).gameObject);
            }

            WeaponId activeId = GetActiveWeaponId();
            if (activeId == WeaponId.None) return;

            WeaponSpec spec = MedievalCatalog.Get(activeId);
            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.42f, 0.27f, 0.14f), 0.05f, 0.2f);
            Material steelMat = MaterialLibrary.GetMaterial(new Color(0.78f, 0.81f, 0.86f), 0.78f, 0.68f);
            Material goldMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.92f, 0.70f, 0.20f), new Color(0.92f, 0.70f, 0.20f), 0.9f);
            Material themeMat = MaterialLibrary.GetMaterial(spec.themeColor, 0.35f, 0.5f);

            switch (activeId)
            {
                case WeaponId.OakenClub:
                    MaterialLibrary.CreatePart("ClubShaft", PrimitiveType.Cylinder, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.38f), new Vector3(0.09f, 0.42f, 0.09f), Quaternion.Euler(90f, 0f, 0f), woodMat, false);
                    break;

                case WeaponId.ArmingSword:
                case WeaponId.SunforgedGreatsword:
                    float len = activeId == WeaponId.SunforgedGreatsword ? 0.68f : 0.48f;
                    Material bladeM = activeId == WeaponId.SunforgedGreatsword ? goldMat : steelMat;
                    MaterialLibrary.CreatePart("Crossguard", PrimitiveType.Cube, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.12f), new Vector3(0.28f, 0.06f, 0.06f), Quaternion.identity, goldMat, false);
                    MaterialLibrary.CreatePart("SwordBlade", PrimitiveType.Cube, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.15f + len), new Vector3(0.08f, 0.03f, len * 2f), Quaternion.identity, bladeM, false);
                    break;

                case WeaponId.RoyalHalberd:
                    MaterialLibrary.CreatePart("PoleShaft", PrimitiveType.Cylinder, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.65f), new Vector3(0.06f, 0.85f, 0.06f), Quaternion.Euler(90f, 0f, 0f), woodMat, false);
                    MaterialLibrary.CreatePart("AxeHead", PrimitiveType.Cube, _weaponHandRoot,
                        new Vector3(0f, 0.14f, 1.25f), new Vector3(0.05f, 0.34f, 0.28f), Quaternion.identity, steelMat, false);
                    break;

                case WeaponId.HuntingShortbow:
                case WeaponId.WarLongbow:
                    float bowSize = activeId == WeaponId.WarLongbow ? 0.66f : 0.48f;
                    MaterialLibrary.CreatePart("BowArc", PrimitiveType.Cylinder, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.22f), new Vector3(0.06f, bowSize, 0.06f), Quaternion.identity, woodMat, false);
                    break;

                case WeaponId.HeavyArbalest:
                case WeaponId.DwarvenRepeater:
                    MaterialLibrary.CreatePart("Stock", PrimitiveType.Cube, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.28f), new Vector3(0.10f, 0.12f, 0.62f), Quaternion.identity, woodMat, false);
                    MaterialLibrary.CreatePart("SteelProd", PrimitiveType.Cube, _weaponHandRoot,
                        new Vector3(0f, 0.04f, 0.56f), new Vector3(0.68f, 0.05f, 0.08f), Quaternion.identity, themeMat, false);
                    break;

                default:
                    MaterialLibrary.CreatePart("HeldFlaskOrPotion", PrimitiveType.Sphere, _weaponHandRoot,
                        new Vector3(0f, 0f, 0.18f), new Vector3(0.22f, 0.26f, 0.22f), Quaternion.identity, themeMat, false);
                    break;
            }
        }

        public WeaponId GetActiveWeaponId()
        {
            if (ActiveSlot < 0 || ActiveSlot >= Loadout.Length) return WeaponId.OakenClub;
            return Loadout[ActiveSlot];
        }

        public void SelectSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 3);
            if (ActiveSlot == slotIndex) return;
            ActiveSlot = slotIndex;
            BowDrawCharge = 0f;
            RefreshHeldWeaponVisual();
        }

        public void JumpFromAirship(Vector3 shipPosition)
        {
            if (CurrentDropState != DropState.AboardAirship) return;
            CurrentDropState = DropState.Freefall;
            transform.position = shipPosition + new Vector3(Random.Range(-2.5f, 2.5f), -3.2f, Random.Range(-2.5f, 2.5f));
            _verticalVelocity = -22f;
            _cc.enabled = true;
            if (IsPlayer) MedievalAudio.PlayGlider();
        }

        public void DeployGlider()
        {
            if (CurrentDropState != DropState.Freefall) return;
            CurrentDropState = DropState.Gliding;
            if (_gliderRoot != null) _gliderRoot.gameObject.SetActive(true);
            if (IsPlayer) MedievalAudio.PlayGlider();
        }

        public void TickMovement(Vector3 worldMoveDir, float desiredYaw, bool wantsSprint, bool wantsJumpOrDive, float dt, IslandWorldGenerator world)
        {
            if (!IsAlive) return;

            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_swingAnimTimer > 0f) _swingAnimTimer -= dt;

            transform.rotation = Quaternion.Euler(0f, desiredYaw, 0f);

            if (CurrentDropState == DropState.AboardAirship)
            {
                return;
            }

            float groundY = world.GetGroundHeight(transform.position.x, transform.position.z);
            float heightAboveGround = transform.position.y - groundY;

            // Freefall & Glider flight physics
            if (CurrentDropState == DropState.Freefall || CurrentDropState == DropState.Gliding)
            {
                if (CurrentDropState == DropState.Freefall)
                {
                    if (wantsJumpOrDive && heightAboveGround < 145f)
                    {
                        DeployGlider();
                    }
                    else if (heightAboveGround <= 52f)
                    {
                        DeployGlider(); // Safety auto-deploy
                    }
                }

                float horizSpeed = CurrentDropState == DropState.Gliding ? 25f : 19f;
                float sinkRate = CurrentDropState == DropState.Gliding ? -11.5f : (wantsSprint ? -38f : -26f);

                Vector3 moveDir = worldMoveDir.sqrMagnitude > 0.01f ? worldMoveDir.normalized : transform.forward;
                Vector3 airVel = moveDir * horizSpeed + Vector3.up * sinkRate;
                _cc.Move(airVel * dt);

                // Clamp to island bounds
                Vector3 p = transform.position;
                p.x = Mathf.Clamp(p.x, -540f, 540f);
                p.z = Mathf.Clamp(p.z, -540f, 540f);

                float newGroundY = world.GetGroundHeight(p.x, p.z);
                bool hasLanded = false;
                float landedFloorY = newGroundY;

                // Check 1: CharacterController grounded on castle floor, courtyard, or roof collider
                if (_cc.isGrounded)
                {
                    hasLanded = true;
                    landedFloorY = p.y;
                }
                // Check 2: Downward raycast for castle structures/floors/bridges
                else if (Physics.Raycast(p + Vector3.up * 0.4f, Vector3.down, out RaycastHit landHit, 1.25f))
                {
                    if (landHit.collider.GetComponentInParent<CombatantActor>() == null)
                    {
                        hasLanded = true;
                        landedFloorY = landHit.point.y;
                    }
                }
                // Check 3: Base terrain height
                else if (p.y <= newGroundY + 0.35f)
                {
                    hasLanded = true;
                    landedFloorY = newGroundY;
                }

                if (hasLanded)
                {
                    p.y = Mathf.Max(landedFloorY, newGroundY);
                    transform.position = p;
                    CurrentDropState = DropState.Grounded;
                    _verticalVelocity = -2f;
                    if (_gliderRoot != null) _gliderRoot.gameObject.SetActive(false);
                }
                else
                {
                    transform.position = p;
                }
                return;
            }

            // Grounded Movement
            bool isSprinting = wantsSprint && worldMoveDir.sqrMagnitude > 0.05f && !IsBlocking && !IsAimingOrDrawing && Stamina > 5f;
            if (isSprinting)
            {
                Stamina = Mathf.Max(0f, Stamina - 16f * dt);
            }
            else
            {
                Stamina = Mathf.Min(MaxStamina, Stamina + 14f * dt);
            }

            float baseSpeed = 7.4f;
            if (isSprinting) baseSpeed = 10.6f;
            else if (IsBlocking || IsAimingOrDrawing) baseSpeed = 4.6f;

            // Keep above water or slow in shallows
            if (transform.position.y < MedievalCatalog.WaterLevel + 0.4f)
            {
                baseSpeed *= 0.72f;
            }

            bool grounded = _cc.isGrounded;
            float surfaceBelowY = world.GetGroundHeight(transform.position.x, transform.position.z);
            if (!grounded)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out RaycastHit gHit, 0.95f))
                {
                    if (gHit.collider.GetComponentInParent<CombatantActor>() == null)
                    {
                        grounded = true;
                        surfaceBelowY = Mathf.Max(surfaceBelowY, gHit.point.y);
                    }
                }
                else if (transform.position.y - surfaceBelowY <= 0.28f)
                {
                    grounded = true;
                }
            }

            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -3.5f;
            }

            if (grounded && wantsJumpOrDive && Stamina >= 12f)
            {
                _verticalVelocity = 8.4f;
                Stamina -= 12f;
            }

            _verticalVelocity -= 21f * dt;

            Vector3 displacement = (worldMoveDir * baseSpeed + Vector3.up * _verticalVelocity) * dt;
            _cc.Move(displacement);

            // Prevent falling below procedural terrain mesh surface or castle structure floors
            Vector3 posAfter = transform.position;
            posAfter.x = Mathf.Clamp(posAfter.x, -555f, 555f);
            posAfter.z = Mathf.Clamp(posAfter.z, -555f, 555f);
            float minSurfaceY = world.GetGroundHeight(posAfter.x, posAfter.z);
            if (Physics.Raycast(posAfter + Vector3.up * 1.2f, Vector3.down, out RaycastHit fHit, 3.5f))
            {
                if (fHit.collider.GetComponentInParent<CombatantActor>() == null)
                {
                    minSurfaceY = Mathf.Max(minSurfaceY, fHit.point.y);
                }
            }
            if (posAfter.y < minSurfaceY)
            {
                posAfter.y = minSurfaceY;
                _verticalVelocity = Mathf.Max(0f, _verticalVelocity);
                transform.position = posAfter;
            }

            AnimateLimbs(worldMoveDir.magnitude * (isSprinting ? 1.4f : 1.0f), dt);
        }

        private void AnimateLimbs(float moveSpeedRatio, float dt)
        {
            if (_legL == null || _legR == null || _armL == null || _armR == null) return;

            if (moveSpeedRatio > 0.08f)
            {
                _walkCycle += dt * 9.5f * moveSpeedRatio;
                float swing = Mathf.Sin(_walkCycle) * 34f * Mathf.Clamp01(moveSpeedRatio);
                _legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
                _legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }
            else
            {
                _legL.localRotation = Quaternion.Lerp(_legL.localRotation, Quaternion.identity, dt * 10f);
                _legR.localRotation = Quaternion.Lerp(_legR.localRotation, Quaternion.identity, dt * 10f);
            }

            // Shield Arm Pose
            if (IsBlocking)
            {
                _armL.localRotation = Quaternion.Euler(-65f, 38f, 0f);
            }
            else
            {
                _armL.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // Weapon Arm Pose (Melee Swing vs Bow Aim vs Idle)
            if (_swingAnimTimer > 0f)
            {
                float norm = _swingAnimTimer / 0.32f;
                float slashPitch = Mathf.Lerp(-15f, -115f, Mathf.Sin(norm * Mathf.PI));
                _armR.localRotation = Quaternion.Euler(slashPitch, -24f, 0f);
            }
            else if (IsAimingOrDrawing)
            {
                _armR.localRotation = Quaternion.Euler(-85f + AimPitchDegrees, 0f, 0f);
            }
            else
            {
                _armR.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }
        }

        public bool TryPerformPrimaryAction(Vector3 aimOrigin, Vector3 aimDirection, GameMatchManager match)
        {
            if (!IsAlive || CurrentDropState != DropState.Grounded || _attackCooldownTimer > 0f)
            {
                return false;
            }

            WeaponId weaponId = GetActiveWeaponId();
            if (weaponId == WeaponId.None)
            {
                SelectSlot(0);
                weaponId = GetActiveWeaponId();
            }

            WeaponSpec spec = MedievalCatalog.Get(weaponId);

            switch (spec.slotType)
            {
                case ItemSlotType.Melee:
                    _attackCooldownTimer = spec.attackCooldown;
                    _swingAnimTimer = 0.32f;
                    if (IsPlayer) MedievalAudio.PlayMeleeSwing();
                    match.ExecuteMeleeCleave(this, spec);
                    return true;

                case ItemSlotType.Ranged:
                    if (ArrowAmmo <= 0)
                    {
                        // Auto switch to melee if out of arrows
                        SelectSlot(0);
                        return false;
                    }
                    ArrowAmmo--;
                    _attackCooldownTimer = spec.attackCooldown;
                    float chargeMultiplier = (weaponId == WeaponId.HuntingShortbow || weaponId == WeaponId.WarLongbow)
                        ? Mathf.Lerp(0.68f, 1.15f, Mathf.Clamp01(BowDrawCharge))
                        : 1.0f;
                    BowDrawCharge = 0f;

                    if (IsPlayer)
                    {
                        if (weaponId == WeaponId.HeavyArbalest || weaponId == WeaponId.DwarvenRepeater)
                            MedievalAudio.PlayCrossbow();
                        else
                            MedievalAudio.PlayBow();
                    }

                    match.Projectiles.SpawnProjectile(
                        this,
                        spec,
                        aimOrigin,
                        aimDirection.normalized * (spec.rangeOrVelocity * chargeMultiplier),
                        false);
                    return true;

                case ItemSlotType.Throwable:
                    if (SlotCounts[ActiveSlot] <= 0) return false;
                    SlotCounts[ActiveSlot]--;
                    _attackCooldownTimer = spec.attackCooldown;
                    _swingAnimTimer = 0.28f;
                    if (IsPlayer) MedievalAudio.PlayMeleeSwing();

                    bool isWildfire = weaponId == WeaponId.AlchemicalFireFlask;
                    match.Projectiles.SpawnProjectile(
                        this,
                        spec,
                        aimOrigin,
                        (aimDirection.normalized + Vector3.up * 0.14f).normalized * spec.rangeOrVelocity,
                        isWildfire);

                    if (SlotCounts[ActiveSlot] <= 0)
                    {
                        Loadout[ActiveSlot] = WeaponId.None;
                        RefreshHeldWeaponVisual();
                    }
                    return true;

                case ItemSlotType.Alchemy:
                    return TryUseHealingItem();
            }

            return false;
        }

        public bool TryUseHealingItem()
        {
            if (!IsAlive || CurrentDropState != DropState.Grounded) return false;
            if (Loadout[3] == WeaponId.None || SlotCounts[3] <= 0) return false;
            if (Health >= MaxHealth - 0.5f && ArmorPoints >= MaxArmorPoints - 0.5f) return false;

            WeaponSpec potion = MedievalCatalog.Get(Loadout[3]);
            Health = Mathf.Min(MaxHealth, Health + potion.healAmount);
            if (potion.armorRestoreAmount > 0 && MaxArmorPoints > 0f)
            {
                ArmorPoints = Mathf.Min(MaxArmorPoints, ArmorPoints + potion.armorRestoreAmount);
            }

            SlotCounts[3]--;
            _attackCooldownTimer = potion.attackCooldown;
            if (IsPlayer) MedievalAudio.PlayPotion();

            if (SlotCounts[3] <= 0)
            {
                Loadout[3] = WeaponId.None;
                if (ActiveSlot == 3) SelectSlot(0);
            }
            return true;
        }

        public bool TryPickupLoot(GroundLootItem item)
        {
            if (item == null || item.collected || !IsAlive) return false;
            WeaponSpec spec = MedievalCatalog.Get(item.weaponId);

            switch (spec.slotType)
            {
                case ItemSlotType.ArrowBundle:
                    ArrowAmmo = Mathf.Min(99, ArrowAmmo + item.count);
                    item.collected = true;
                    item.gameObject.SetActive(false);
                    if (IsPlayer) MedievalAudio.PlayLoot();
                    return true;

                case ItemSlotType.ArmorUpgrade:
                    int newTier = spec.armorTierValue;
                    int newMax = MedievalCatalog.GetMaxArmorForTier(newTier);
                    if (newTier > ArmorTier || ArmorPoints < newMax)
                    {
                        ArmorTier = Mathf.Max(ArmorTier, newTier);
                        MaxArmorPoints = MedievalCatalog.GetMaxArmorForTier(ArmorTier);
                        ArmorPoints = MaxArmorPoints;
                        RefreshArmorVisuals();
                        item.collected = true;
                        item.gameObject.SetActive(false);
                        if (IsPlayer) MedievalAudio.PlayLoot();
                        return true;
                    }
                    return false;

                case ItemSlotType.Melee:
                    EquipOrUpgradeSlot(0, item);
                    return true;

                case ItemSlotType.Ranged:
                    EquipOrUpgradeSlot(1, item);
                    ArrowAmmo = Mathf.Min(99, ArrowAmmo + 10);
                    return true;

                case ItemSlotType.Throwable:
                    EquipOrStackSlot(2, item, 6);
                    return true;

                case ItemSlotType.Alchemy:
                    EquipOrStackSlot(3, item, 6);
                    return true;
            }

            return false;
        }

        private void EquipOrUpgradeSlot(int slot, GroundLootItem item)
        {
            Loadout[slot] = item.weaponId;
            SlotCounts[slot] = 1;
            item.collected = true;
            item.gameObject.SetActive(false);
            if (IsPlayer) MedievalAudio.PlayLoot();
            if (ActiveSlot == slot || (slot == 1 && Loadout[0] == WeaponId.OakenClub))
            {
                ActiveSlot = slot;
                RefreshHeldWeaponVisual();
            }
        }

        private void EquipOrStackSlot(int slot, GroundLootItem item, int maxStack)
        {
            if (Loadout[slot] == item.weaponId)
            {
                SlotCounts[slot] = Mathf.Min(maxStack, SlotCounts[slot] + item.count);
            }
            else
            {
                Loadout[slot] = item.weaponId;
                SlotCounts[slot] = Mathf.Min(maxStack, item.count);
            }
            item.collected = true;
            item.gameObject.SetActive(false);
            if (IsPlayer) MedievalAudio.PlayLoot();
            if (ActiveSlot == slot) RefreshHeldWeaponVisual();
        }

        public void ApplyDamage(float rawDamage, CombatantActor attacker, string weaponLabel, bool isHeadshot, bool ignoreArmor, GameMatchManager match)
        {
            if (!IsAlive) return;

            float finalDamage = rawDamage;
            if (isHeadshot) finalDamage *= 1.65f;

            // Shield Block mitigation
            if (IsBlocking && !ignoreArmor && attacker != null)
            {
                Vector3 toAttacker = (attacker.transform.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, toAttacker) > 0.15f)
                {
                    finalDamage *= 0.40f;
                    if (IsPlayer || attacker.IsPlayer) MedievalAudio.PlayHitArmor();
                }
            }

            bool hitArmor = false;
            if (!ignoreArmor && ArmorPoints > 0f && ArmorTier > 0)
            {
                hitArmor = true;
                float dr = MedievalCatalog.GetDamageReductionForTier(ArmorTier);
                float absorbed = finalDamage * dr;
                ArmorPoints = Mathf.Max(0f, ArmorPoints - absorbed * 0.85f);
                finalDamage -= absorbed;
                if (ArmorPoints <= 0f)
                {
                    RefreshArmorVisuals();
                }
            }

            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
            Health -= dmgInt;

            if (attacker != null)
            {
                attacker.TotalDamageDealt += dmgInt;
                if (attacker.IsPlayer || IsPlayer)
                {
                    if (hitArmor) MedievalAudio.PlayHitArmor();
                    else MedievalAudio.PlayHitFlesh();
                    match.RecordFloatingDamage(transform.position + Vector3.up * 1.9f, dmgInt, isHeadshot, hitArmor);
                }
            }

            if (Health <= 0f)
            {
                Health = 0f;
                IsAlive = false;
                _cc.enabled = false;
                if (VisualRoot != null)
                {
                    VisualRoot.localRotation = Quaternion.Euler(-86f, 0f, 0f);
                    VisualRoot.localPosition = new Vector3(0f, 0.25f, 0f);
                }
                match.OnCombatantEliminated(this, attacker, weaponLabel);
            }
        }

        public void TickZoneAndFireHazards(float dt, BlightZoneController zone, GameMatchManager match)
        {
            if (!IsAlive || CurrentDropState == DropState.AboardAirship) return;

            _zoneTickTimer += dt;
            if (_zoneTickTimer >= 1.0f)
            {
                _zoneTickTimer = 0f;
                if (zone.IsOutsideZone(transform.position))
                {
                    ApplyDamage(zone.DamagePerSecond, null, "The Cursed Blight", false, true, match);
                }
            }
        }
    }
}
