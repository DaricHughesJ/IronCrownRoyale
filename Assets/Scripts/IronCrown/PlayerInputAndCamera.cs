using UnityEngine;

namespace IronCrown
{
    public class PlayerInputAndCamera : MonoBehaviour
    {
        public bool FirstPersonMode;
        public bool IsMapOpen;
        public GroundLootItem NearestPickupCandidate { get; private set; }

        // Mobile / Virtual Controls State (written by HUD touch controls or read by player controller)
        public Vector2 MobileMoveJoystick;
        public Vector2 MobileLookDelta;
        public bool MobileSprintActive;
        public bool MobileAimToggle;
        public bool MobileBlockHeld;
        public bool MobileFirePressed;
        public bool MobileFireHeld;
        public bool MobileJumpPressed;
        public bool MobilePickupPressed;
        public bool MobileHealPressed;

        private Camera _cam;
        private float _yaw = 180f;
        private float _pitch = 12f;
        private float _menuOrbitAngle;

        public Camera ActiveCamera => _cam;

        public void InitializeCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _cam.nearClipPlane = 0.2f;
            _cam.farClipPlane = 1500f;
            _cam.fieldOfView = 62f;
            _cam.backgroundColor = new Color(0.52f, 0.65f, 0.78f);
        }

        public void ResetViewAngles(float startYaw)
        {
            _yaw = startYaw;
            _pitch = 14f;
            IsMapOpen = false;
            MobileAimToggle = false;
        }

        public bool ShouldShowMobileTouchHud(ControlLayoutMode mode)
        {
            if (mode == ControlLayoutMode.MobileTouch) return true;
            if (mode == ControlLayoutMode.PCKeyboardMouse) return false;
            return Application.isMobilePlatform || Input.touchSupported;
        }

        public void TickPlayerAndCamera(float dt, GameMatchManager match)
        {
            if (_cam == null) InitializeCamera();

            // Menu Cinematic Orbit Camera around Highkeep Citadel
            if (match.CurrentPhase == MatchPhase.Menu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _menuOrbitAngle += dt * 7.5f;
                float rad = _menuOrbitAngle * Mathf.Deg2Rad;
                Vector2 citadelXZ = MedievalCatalog.Landmarks[0].worldXZ;
                Vector3 focus = new Vector3(citadelXZ.x, 38f, citadelXZ.y);
                Vector3 camPos = focus + new Vector3(Mathf.Cos(rad) * 115f, 36f, Mathf.Sin(rad) * 115f);
                _cam.transform.position = camPos;
                _cam.transform.LookAt(focus);
                _cam.fieldOfView = 55f;
                return;
            }

            // Toggle Full Parchment Map (M or Tab)
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
            {
                IsMapOpen = !IsMapOpen;
            }

            // Toggle First-Person / Third-Person Camera (V)
            if (Input.GetKeyDown(KeyCode.V))
            {
                FirstPersonMode = !FirstPersonMode;
            }

            // Escape unlocks cursor on PC
            bool mobileHudActive = ShouldShowMobileTouchHud(match.SelectedControlLayout);
            if (!mobileHudActive && match.CurrentPhase == MatchPhase.InProgress && !IsMapOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    if ( Input.mousePosition.y > 110f && Input.mousePosition.x < Screen.width - 240f)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            CombatantActor player = match.PlayerActor;
            if (player == null) return;

            // Camera Look Input (PC Mouse + Mobile Drag)
            bool pointerLocked = Cursor.lockState == CursorLockMode.Locked;
            float lookX = 0f;
            float lookY = 0f;

            if (pointerLocked || (!mobileHudActive && Input.GetMouseButton(1)))
            {
                lookX += Input.GetAxis("Mouse X") * 2.6f;
                lookY += Input.GetAxis("Mouse Y") * 2.4f;
            }

            if (MobileLookDelta.sqrMagnitude > 0.0001f)
            {
                lookX += MobileLookDelta.x * 0.22f;
                lookY += MobileLookDelta.y * 0.20f;
                MobileLookDelta = Vector2.zero;
            }

            _yaw = (_yaw + lookX) % 360f;
            _pitch = Mathf.Clamp(_pitch - lookY, -52f, 65f);

            // If Player is dead, orbit the currently spectated combatant
            if (!player.IsAlive)
            {
                CombatantActor specTarget = match.GetSpectateTarget();
                if (specTarget != null)
                {
                    UpdateFollowCamera(specTarget, dt, false);
                }
                ClearOneShotMobileFlags();
                return;
            }

            // Refresh nearest ground loot pickup candidate within 3.4m
            UpdateNearestLootCandidate(player, match.World);

            // Slot selection keys (1-4 & ScrollWheel)
            if (Input.GetKeyDown(KeyCode.Alpha1)) player.SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) player.SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) player.SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) player.SelectSlot(3);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.02f)
            {
                int dir = scroll > 0f ? -1 : 1;
                player.SelectSlot((player.ActiveSlot + dir + 4) % 4);
            }

            // Quick Heal (H)
            if (Input.GetKeyDown(KeyCode.H) || MobileHealPressed)
            {
                player.TryUseHealingItem();
            }

            // Pickup Loot (E / F or Mobile Pickup button)
            if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || MobilePickupPressed) && NearestPickupCandidate != null)
            {
                player.TryPickupLoot(NearestPickupCandidate);
                UpdateNearestLootCandidate(player, match.World);
            }

            // Jump / Dive from Airship / Deploy Glider (Space or Mobile Jump)
            bool jumpTriggered = Input.GetKeyDown(KeyCode.Space) || MobileJumpPressed;

            if (player.CurrentDropState == DropState.AboardAirship)
            {
                if (jumpTriggered || !match.Airship.IsFlying)
                {
                    player.JumpFromAirship(match.Airship.transform.position);
                }
                UpdateAirshipCamera(match.Airship.transform.position, dt);
                ClearOneShotMobileFlags();
                return;
            }

            // Movement Vector (WASD + Mobile Joystick)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            if (MobileMoveJoystick.sqrMagnitude > 0.01f)
            {
                moveX += MobileMoveJoystick.x;
                moveZ += MobileMoveJoystick.y;
            }
            Vector2 inputXZ = Vector2.ClampMagnitude(new Vector2(moveX, moveZ), 1f);

            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 worldMove = yawRot * new Vector3(inputXZ.x, 0f, inputXZ.y);
            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || MobileSprintActive;

            // Aiming / Shield Blocking (Right Mouse Button or Mobile Aim/Block button)
            WeaponSpec activeSpec = MedievalCatalog.Get(player.GetActiveWeaponId());
            bool rightHeld = (!mobileHudActive && Input.GetMouseButton(1)) || MobileAimToggle || MobileBlockHeld;

            if (activeSpec.slotType == ItemSlotType.Melee)
            {
                player.IsBlocking = rightHeld;
                player.IsAimingOrDrawing = false;
                player.BowDrawCharge = 0f;
            }
            else if (activeSpec.slotType == ItemSlotType.Ranged || activeSpec.slotType == ItemSlotType.Throwable)
            {
                player.IsBlocking = false;
                bool drawingBow = rightHeld || Input.GetMouseButton(0) || MobileFireHeld;
                player.IsAimingOrDrawing = drawingBow;
                if (drawingBow)
                {
                    player.BowDrawCharge = Mathf.Min(1f, player.BowDrawCharge + dt * 1.35f);
                }
            }
            else
            {
                player.IsBlocking = false;
                player.IsAimingOrDrawing = false;
            }

            player.AimPitchDegrees = -_pitch;
            player.TickMovement(worldMove, _yaw, wantsSprint, jumpTriggered, dt, match.World);

            // Update Camera first so aiming ray uses exact camera position
            UpdateFollowCamera(player, dt, player.IsAimingOrDrawing);

            // Attack / Fire Action
            bool fireTriggered = false;
            if (activeSpec.id == WeaponId.HuntingShortbow || activeSpec.id == WeaponId.WarLongbow)
            {
                // Bows fire on release of Left Click or when tapped
                if ((!mobileHudActive && Input.GetMouseButtonUp(0) && !IsMapOpen) || MobileFirePressed)
                {
                    fireTriggered = true;
                }
            }
            else
            {
                if ((!mobileHudActive && Input.GetMouseButton(0) && !IsMapOpen && (pointerLocked || Input.mousePosition.y > 115f)) || MobileFirePressed || MobileFireHeld)
                {
                    fireTriggered = true;
                }
            }

            if (fireTriggered)
            {
                Vector3 aimOrigin = player.transform.position + Vector3.up * 1.52f + player.transform.forward * 0.55f;
                Vector3 aimTarget = _cam.transform.position + _cam.transform.forward * 85f;
                if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, 180f))
                {
                    if (hit.collider.GetComponentInParent<CombatantActor>() != player)
                    {
                        aimTarget = hit.point;
                    }
                }
                Vector3 aimDir = (aimTarget - aimOrigin).normalized;
                player.TryPerformPrimaryAction(aimOrigin, aimDir, match);
            }

            ClearOneShotMobileFlags();
        }

        private void UpdateNearestLootCandidate(CombatantActor player, IslandWorldGenerator world)
        {
            NearestPickupCandidate = null;
            if (player.CurrentDropState != DropState.Grounded) return;

            float bestSq = 3.5f * 3.5f;
            Vector3 p = player.transform.position;
            var list = world.ActiveLoot;
            for (int i = 0; i < list.Count; i++)
            {
                GroundLootItem item = list[i];
                if (item == null || item.collected) continue;
                float dSq = (item.transform.position - p).sqrMagnitude;
                if (dSq < bestSq)
                {
                    bestSq = dSq;
                    NearestPickupCandidate = item;
                }
            }

            // Auto-pickup arrows & armor upgrades when walking directly over them (<= 1.65m)
            if (NearestPickupCandidate != null && bestSq <= 1.65f * 1.65f)
            {
                WeaponSpec spec = MedievalCatalog.Get(NearestPickupCandidate.weaponId);
                if (spec.slotType == ItemSlotType.ArrowBundle || spec.slotType == ItemSlotType.ArmorUpgrade)
                {
                    player.TryPickupLoot(NearestPickupCandidate);
                    NearestPickupCandidate = null;
                }
            }
        }

        private void UpdateAirshipCamera(Vector3 shipPos, float dt)
        {
            Quaternion rot = Quaternion.Euler(Mathf.Clamp(_pitch, -10f, 55f), _yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 8f, -38f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, shipPos + offset, dt * 10f);
            _cam.transform.LookAt(shipPos + Vector3.up * 3.5f);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 64f, dt * 8f);
        }

        private void UpdateFollowCamera(CombatantActor target, float dt, bool isAiming)
        {
            if (target.VisualRoot != null)
            {
                target.HeadTransform.gameObject.SetActive(!FirstPersonMode || !target.IsPlayer);
            }

            Quaternion lookRot = Quaternion.Euler(_pitch, _yaw, 0f);

            if (FirstPersonMode && target.IsPlayer && target.CurrentDropState == DropState.Grounded)
            {
                Vector3 eyePos = target.transform.position + new Vector3(0f, 1.72f, 0f) + target.transform.forward * 0.18f;
                _cam.transform.position = eyePos;
                _cam.transform.rotation = lookRot;
            }
            else
            {
                float dist = (target.CurrentDropState == DropState.Freefall || target.CurrentDropState == DropState.Gliding)
                    ? 8.5f
                    : (isAiming ? 2.65f : 4.5f);
                float shoulderX = isAiming ? 0.68f : 0.55f;
                Vector3 pivot = target.transform.position + new Vector3(0f, 1.62f, 0f);
                Vector3 desiredOffset = lookRot * new Vector3(shoulderX, 0.35f, -dist);
                Vector3 desiredPos = pivot + desiredOffset;

                // Camera collision prevention against castle walls & terrain
                Vector3 dir = desiredPos - pivot;
                float len = dir.magnitude;
                if (len > 0.1f && Physics.SphereCast(pivot, 0.25f, dir / len, out RaycastHit hit, len))
                {
                    if (hit.collider.GetComponentInParent<CombatantActor>() == null)
                    {
                        desiredPos = pivot + (dir / len) * Mathf.Max(0.8f, hit.distance - 0.25f);
                    }
                }

                float minTerrainY = IslandWorldGenerator.SampleProceduralHeight(desiredPos.x, desiredPos.z) + 0.65f;
                if (desiredPos.y < minTerrainY) desiredPos.y = minTerrainY;

                _cam.transform.position = desiredPos;
                _cam.transform.rotation = lookRot;
            }

            float targetFov = isAiming ? 44f : 62f;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, dt * 10f);
        }

        private void ClearOneShotMobileFlags()
        {
            MobileFirePressed = false;
            MobileJumpPressed = false;
            MobilePickupPressed = false;
            MobileHealPressed = false;
        }
    }
}
