using UnityEngine;

namespace IronCrown
{
    public class DwarvenAirship : MonoBehaviour
    {
        public Vector3 StartPoint { get; private set; }
        public Vector3 EndPoint { get; private set; }
        public float FlightProgress { get; private set; }
        public bool IsFlying { get; private set; }

        private const float FlightAltitude = 188f;
        private const float FlightSpeed = 38f;
        private float _totalDistance;
        private Transform _propellerL;
        private Transform _propellerR;

        public void BuildShipVisual()
        {
            Material hullMat = MaterialLibrary.GetMaterial(new Color(0.36f, 0.23f, 0.13f), 0.1f, 0.25f);
            Material brassMat = MaterialLibrary.GetMaterial(new Color(0.78f, 0.58f, 0.22f), 0.72f, 0.62f);
            Material balloonMat = MaterialLibrary.GetMaterial(new Color(0.68f, 0.18f, 0.16f), 0.08f, 0.22f);
            Material stripeMat = MaterialLibrary.GetMaterial(new Color(0.86f, 0.78f, 0.62f), 0.05f, 0.20f);
            Material furnaceMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.96f, 0.45f, 0.12f), new Color(0.96f, 0.42f, 0.10f), 2.0f);

            // Dirigible Balloon Envelope overhead
            MaterialLibrary.CreatePart("DirigibleEnvelope", PrimitiveType.Sphere, transform,
                new Vector3(0f, 11.5f, 0f), new Vector3(14f, 11f, 36f), Quaternion.identity, balloonMat, false);
            MaterialLibrary.CreatePart("HeraldicStripe", PrimitiveType.Sphere, transform,
                new Vector3(0f, 11.5f, 0f), new Vector3(14.3f, 3.2f, 36.2f), Quaternion.identity, stripeMat, false);
            MaterialLibrary.CreatePart("BrassProwCap", PrimitiveType.Sphere, transform,
                new Vector3(0f, 11.5f, 17.5f), new Vector3(6.2f, 5.8f, 6.2f), Quaternion.identity, brassMat, false);

            // Timber & Iron Galleon Hull suspended below
            MaterialLibrary.CreatePart("GalleonDeck", PrimitiveType.Cube, transform,
                new Vector3(0f, 0f, 0f), new Vector3(7.6f, 2.8f, 24f), Quaternion.identity, hullMat, false);
            MaterialLibrary.CreatePart("ProwRam", PrimitiveType.Cube, transform,
                new Vector3(0f, 0.6f, 13.2f), new Vector3(3.4f, 2.2f, 4.5f), Quaternion.identity, brassMat, false);
            MaterialLibrary.CreatePart("SternCastle", PrimitiveType.Cube, transform,
                new Vector3(0f, 2.5f, -9.2f), new Vector3(7.2f, 2.6f, 6.4f), Quaternion.identity, hullMat, false);
            MaterialLibrary.CreatePart("AlchemicalFurnace", PrimitiveType.Cylinder, transform,
                new Vector3(0f, 2.2f, 0f), new Vector3(2.8f, 2.0f, 2.8f), Quaternion.identity, furnaceMat, false);

            // Suspension Rigging Struts
            for (int z = -1; z <= 1; z += 2)
            {
                for (int x = -1; x <= 1; x += 2)
                {
                    MaterialLibrary.CreatePart($"RigStrut_{x}_{z}", PrimitiveType.Cylinder, transform,
                        new Vector3(x * 3.1f, 5.5f, z * 7.5f), new Vector3(0.22f, 5.5f, 0.22f), Quaternion.identity, brassMat, false);
                }
            }

            // Twin Brass Propellers
            _propellerL = new GameObject("PropellerL").transform;
            _propellerL.SetParent(transform, false);
            _propellerL.localPosition = new Vector3(-4.8f, 1.2f, -6.5f);
            MaterialLibrary.CreatePart("BladeA", PrimitiveType.Cube, _propellerL, Vector3.zero, new Vector3(3.8f, 0.45f, 0.18f), Quaternion.identity, brassMat, false);
            MaterialLibrary.CreatePart("BladeB", PrimitiveType.Cube, _propellerL, Vector3.zero, new Vector3(0.45f, 3.8f, 0.18f), Quaternion.identity, brassMat, false);

            _propellerR = new GameObject("PropellerR").transform;
            _propellerR.SetParent(transform, false);
            _propellerR.localPosition = new Vector3(4.8f, 1.2f, -6.5f);
            MaterialLibrary.CreatePart("BladeA", PrimitiveType.Cube, _propellerR, Vector3.zero, new Vector3(3.8f, 0.45f, 0.18f), Quaternion.identity, brassMat, false);
            MaterialLibrary.CreatePart("BladeB", PrimitiveType.Cube, _propellerR, Vector3.zero, new Vector3(0.45f, 3.8f, 0.18f), Quaternion.identity, brassMat, false);
        }

        public void LaunchNewFlightPath()
        {
            float entryAngle = Random.Range(0f, Mathf.PI * 2f);
            // Offset exit angle slightly so the flight path passes near the center/POIs of the island
            float exitAngle = entryAngle + Mathf.PI + Random.Range(-0.42f, 0.42f);
            float flightRadius = 540f;

            StartPoint = new Vector3(Mathf.Cos(entryAngle) * flightRadius, FlightAltitude, Mathf.Sin(entryAngle) * flightRadius);
            EndPoint = new Vector3(Mathf.Cos(exitAngle) * flightRadius, FlightAltitude, Mathf.Sin(exitAngle) * flightRadius);
            _totalDistance = Vector3.Distance(StartPoint, EndPoint);

            FlightProgress = 0f;
            IsFlying = true;
            gameObject.SetActive(true);
            transform.position = StartPoint;
            transform.rotation = Quaternion.LookRotation((EndPoint - StartPoint).normalized, Vector3.up);
        }

        public void TickAirship(float dt)
        {
            if (!IsFlying) return;

            FlightProgress += (FlightSpeed * dt) / Mathf.Max(1f, _totalDistance);
            if (FlightProgress >= 1f)
            {
                FlightProgress = 1f;
                IsFlying = false;
                gameObject.SetActive(false);
                return;
            }

            transform.position = Vector3.Lerp(StartPoint, EndPoint, FlightProgress);
            if (_propellerL != null) _propellerL.Rotate(0f, 0f, 540f * dt, Space.Self);
            if (_propellerR != null) _propellerR.Rotate(0f, 0f, -540f * dt, Space.Self);
        }
    }

    public class BlightZoneController : MonoBehaviour
    {
        public Vector2 CurrentCenterXZ { get; private set; }
        public float CurrentRadius { get; private set; }
        public Vector2 TargetCenterXZ { get; private set; }
        public float TargetRadius { get; private set; }

        public int CurrentStage { get; private set; }
        public bool IsShrinking { get; private set; }
        public float PhaseTimer { get; private set; }
        public float PhaseDuration { get; private set; }
        public float DamagePerSecond { get; private set; }

        private Vector2 _shrinkStartCenter;
        private float _shrinkStartRadius;
        private Transform _stormCylinder;

        private static readonly float[] StageRadii = { 620f, 410f, 260f, 145f, 70f, 18f };
        private static readonly float[] StageWaitTimes = { 35f, 32f, 28f, 24f, 20f, 18f };
        private static readonly float[] StageShrinkTimes = { 30f, 28f, 24f, 22f, 18f, 16f };
        private static readonly float[] StageDps = { 2.0f, 3.5f, 6.0f, 10.0f, 16.0f, 24.0f };

        public void InitializeZone()
        {
            CurrentStage = 0;
            CurrentCenterXZ = Vector2.zero;
            CurrentRadius = StageRadii[0];

            // Pick first safe circle centered near one of the inner regions
            TargetCenterXZ = Random.insideUnitCircle * 145f;
            TargetRadius = StageRadii[1];

            IsShrinking = false;
            PhaseDuration = StageWaitTimes[0];
            PhaseTimer = PhaseDuration;
            DamagePerSecond = StageDps[0];

            if (_stormCylinder == null)
            {
                BuildStormWallMesh();
            }
            UpdateWallTransform();
        }

        private void BuildStormWallMesh()
        {
            // Custom hollow open-ended cylinder mesh so players inside see the glowing amber/crimson Blight wall around the perimeter
            GameObject go = new GameObject("CursedBlightWall");
            go.transform.SetParent(transform, false);
            _stormCylinder = go.transform;

            int segments = 64;
            Vector3[] verts = new Vector3[(segments + 1) * 2];
            int[] tris = new int[segments * 12]; // Double-sided wall

            for (int i = 0; i <= segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                float cx = Mathf.Cos(ang);
                float cz = Mathf.Sin(ang);
                verts[i * 2] = new Vector3(cx, 0f, cz);
                verts[i * 2 + 1] = new Vector3(cx, 1f, cz);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;

                // Inward facing
                tris[t++] = b0; tris[t++] = t0; tris[t++] = b1;
                tris[t++] = b1; tris[t++] = t0; tris[t++] = t1;
                // Outward facing
                tris[t++] = b0; tris[t++] = b1; tris[t++] = t0;
                tris[t++] = b1; tris[t++] = t1; tris[t++] = t0;
            }

            Mesh m = new Mesh();
            m.name = "HollowBlightCylinder";
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = m;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.GetTransparentMaterial(new Color(0.88f, 0.34f, 0.12f, 0.28f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void TickZone(float dt)
        {
            PhaseTimer -= dt;
            if (IsShrinking)
            {
                float t = 1f - Mathf.Clamp01(PhaseTimer / Mathf.Max(0.1f, PhaseDuration));
                CurrentCenterXZ = Vector2.Lerp(_shrinkStartCenter, TargetCenterXZ, t);
                CurrentRadius = Mathf.Lerp(_shrinkStartRadius, TargetRadius, t);

                if (PhaseTimer <= 0f)
                {
                    CurrentCenterXZ = TargetCenterXZ;
                    CurrentRadius = TargetRadius;
                    IsShrinking = false;

                    if (CurrentStage < StageRadii.Length - 2)
                    {
                        CurrentStage++;
                        float nextRadius = StageRadii[CurrentStage + 1];
                        float maxOffset = Mathf.Max(0f, CurrentRadius - nextRadius);
                        TargetCenterXZ = CurrentCenterXZ + Random.insideUnitCircle * (maxOffset * 0.65f);
                        TargetRadius = nextRadius;
                        PhaseDuration = StageWaitTimes[CurrentStage];
                        PhaseTimer = PhaseDuration;
                        DamagePerSecond = StageDps[CurrentStage];
                    }
                    else
                    {
                        PhaseDuration = 999f;
                        PhaseTimer = 999f;
                    }
                }
            }
            else
            {
                if (PhaseTimer <= 0f)
                {
                    IsShrinking = true;
                    _shrinkStartCenter = CurrentCenterXZ;
                    _shrinkStartRadius = CurrentRadius;
                    PhaseDuration = StageShrinkTimes[Mathf.Min(CurrentStage, StageShrinkTimes.Length - 1)];
                    PhaseTimer = PhaseDuration;
                    MedievalAudio.PlayWarHorn();
                }
            }

            UpdateWallTransform();
        }

        private void UpdateWallTransform()
        {
            if (_stormCylinder == null) return;
            _stormCylinder.position = new Vector3(CurrentCenterXZ.x, 0f, CurrentCenterXZ.y);
            _stormCylinder.localScale = new Vector3(CurrentRadius, 190f, CurrentRadius);
        }

        public bool IsOutsideZone(Vector3 worldPos)
        {
            Vector2 p = new Vector2(worldPos.x, worldPos.z);
            return Vector2.Distance(p, CurrentCenterXZ) > CurrentRadius;
        }
    }
}
