using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public class ActiveProjectile
    {
        public CombatantActor owner;
        public WeaponSpec weaponSpec;
        public Transform visual;
        public Vector3 position;
        public Vector3 velocity;
        public float gravity;
        public float spawnTime;
        public bool spawnsWildfireOnImpact;
        public bool active;
    }

    public class WildfirePool
    {
        public CombatantActor owner;
        public Transform visual;
        public Vector3 position;
        public float radius;
        public float remainingTime;
        public float tickTimer;
    }

    public class ProjectileManager : MonoBehaviour
    {
        private readonly List<ActiveProjectile> _projectiles = new List<ActiveProjectile>();
        private readonly List<WildfirePool> _firePools = new List<WildfirePool>();
        private Transform _projectileRoot;

        public void InitializeProjectiles()
        {
            ClearAll();
            if (_projectileRoot == null)
            {
                _projectileRoot = new GameObject("ProjectilesAndHazardsRoot").transform;
                _projectileRoot.SetParent(transform, false);
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].visual != null) Destroy(_projectiles[i].visual.gameObject);
            }
            _projectiles.Clear();

            for (int i = 0; i < _firePools.Count; i++)
            {
                if (_firePools[i].visual != null) Destroy(_firePools[i].visual.gameObject);
            }
            _firePools.Clear();
        }

        public void SpawnProjectile(
            CombatantActor shooter,
            WeaponSpec spec,
            Vector3 startPos,
            Vector3 initialVelocity,
            bool isWildfire)
        {
            GameObject go = new GameObject("Proj_" + spec.shortCode);
            go.transform.SetParent(_projectileRoot, false);
            go.transform.position = startPos;

            Material woodMat = MaterialLibrary.GetMaterial(new Color(0.48f, 0.34f, 0.18f), 0.05f, 0.2f);
            Material steelMat = MaterialLibrary.GetMaterial(new Color(0.84f, 0.87f, 0.92f), 0.8f, 0.7f);
            Material featherMat = MaterialLibrary.GetEmissiveMaterial(shooter.HeraldicColor, shooter.HeraldicColor, 0.9f);
            Material fireMat = MaterialLibrary.GetEmissiveMaterial(new Color(0.96f, 0.45f, 0.10f), new Color(0.98f, 0.42f, 0.08f), 1.8f);

            if (isWildfire)
            {
                MaterialLibrary.CreatePart("Flask", PrimitiveType.Sphere, go.transform,
                    Vector3.zero, new Vector3(0.32f, 0.36f, 0.32f), Quaternion.identity, fireMat, false);
            }
            else if (spec.id == WeaponId.FranciscaAxes)
            {
                MaterialLibrary.CreatePart("Handle", PrimitiveType.Cylinder, go.transform,
                    Vector3.zero, new Vector3(0.06f, 0.32f, 0.06f), Quaternion.Euler(90f, 0f, 0f), woodMat, false);
                MaterialLibrary.CreatePart("Blade", PrimitiveType.Cube, go.transform,
                    new Vector3(0f, 0.12f, 0.18f), new Vector3(0.05f, 0.28f, 0.24f), Quaternion.identity, steelMat, false);
            }
            else
            {
                // Arrow or Crossbow Bolt
                MaterialLibrary.CreatePart("Shaft", PrimitiveType.Cylinder, go.transform,
                    Vector3.zero, new Vector3(0.04f, 0.48f, 0.04f), Quaternion.Euler(90f, 0f, 0f), woodMat, false);
                MaterialLibrary.CreatePart("Broadhead", PrimitiveType.Cube, go.transform,
                    new Vector3(0f, 0f, 0.48f), new Vector3(0.07f, 0.07f, 0.16f), Quaternion.identity, steelMat, false);
                MaterialLibrary.CreatePart("Fletching", PrimitiveType.Cube, go.transform,
                    new Vector3(0f, 0f, -0.42f), new Vector3(0.12f, 0.12f, 0.18f), Quaternion.identity, featherMat, false);
            }

            if (initialVelocity.sqrMagnitude > 0.01f)
            {
                go.transform.rotation = Quaternion.LookRotation(initialVelocity.normalized, Vector3.up);
            }

            _projectiles.Add(new ActiveProjectile
            {
                owner = shooter,
                weaponSpec = spec,
                visual = go.transform,
                position = startPos,
                velocity = initialVelocity,
                gravity = Mathf.Max(6f, spec.projectileGravity),
                spawnTime = Time.time,
                spawnsWildfireOnImpact = isWildfire,
                active = true
            });
        }

        public void TickProjectiles(float dt, GameMatchManager match)
        {
            float now = Time.time;

            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ActiveProjectile p = _projectiles[i];
                if (!p.active || now - p.spawnTime > 6.5f)
                {
                    if (p.visual != null) Destroy(p.visual.gameObject);
                    _projectiles.RemoveAt(i);
                    continue;
                }

                Vector3 prevPos = p.position;
                p.velocity += Vector3.down * (p.gravity * dt);
                Vector3 nextPos = prevPos + p.velocity * dt;

                // Check combatant hits along segment prevPos -> nextPos
                CombatantActor hitActor = null;
                bool isHeadshot = false;
                Vector3 hitPoint = nextPos;

                List<CombatantActor> actors = match.AllCombatants;
                for (int a = 0; a < actors.Count; a++)
                {
                    CombatantActor target = actors[a];
                    if (target == null || !target.IsAlive || target == p.owner) continue;
                    if (target.CurrentDropState == DropState.AboardAirship) continue;

                    Vector3 actorBase = target.transform.position;
                    if (Mathf.Abs(actorBase.x - nextPos.x) > 2.2f || Mathf.Abs(actorBase.z - nextPos.z) > 2.2f)
                        continue;

                    if (nextPos.y >= actorBase.y - 0.1f && nextPos.y <= actorBase.y + 2.05f)
                    {
                        Vector2 dXZ = new Vector2(nextPos.x - actorBase.x, nextPos.z - actorBase.z);
                        if (dXZ.sqrMagnitude <= 0.62f * 0.62f)
                        {
                            hitActor = target;
                            isHeadshot = (nextPos.y - actorBase.y) >= 1.45f;
                            hitPoint = nextPos;
                            break;
                        }
                    }
                }

                if (hitActor != null)
                {
                    float dist = p.owner != null ? Vector3.Distance(p.owner.transform.position, hitActor.transform.position) : 0f;
                    if (p.owner != null && dist > p.owner.LongestHitDistance)
                    {
                        p.owner.LongestHitDistance = dist;
                    }

                    bool piercesArmor = p.weaponSpec.id == WeaponId.HeavyArbalest;
                    hitActor.ApplyDamage(p.weaponSpec.baseDamage, p.owner, p.weaponSpec.displayName, isHeadshot, piercesArmor, match);

                    if (p.spawnsWildfireOnImpact)
                    {
                        SpawnWildfirePool(p.owner, match.World.GetGroundHeight(hitPoint.x, hitPoint.z), hitPoint);
                    }

                    if (p.visual != null) Destroy(p.visual.gameObject);
                    _projectiles.RemoveAt(i);
                    continue;
                }

                // Check ground / building collision
                float groundY = match.World.GetGroundHeight(nextPos.x, nextPos.z);
                bool hitEnvironment = nextPos.y <= groundY;
                if (!hitEnvironment)
                {
                    Vector3 seg = nextPos - prevPos;
                    float segLen = seg.magnitude;
                    if (segLen > 0.001f && Physics.Raycast(prevPos, seg / segLen, out RaycastHit rh, segLen))
                    {
                        if (rh.collider.GetComponentInParent<CombatantActor>() == null)
                        {
                            hitEnvironment = true;
                            nextPos = rh.point;
                        }
                    }
                }

                if (hitEnvironment)
                {
                    if (p.spawnsWildfireOnImpact)
                    {
                        float gy = match.World.GetGroundHeight(nextPos.x, nextPos.z);
                        SpawnWildfirePool(p.owner, gy, nextPos);
                    }
                    if (p.visual != null) Destroy(p.visual.gameObject);
                    _projectiles.RemoveAt(i);
                    continue;
                }

                p.position = nextPos;
                if (p.visual != null)
                {
                    p.visual.position = nextPos;
                    if (p.velocity.sqrMagnitude > 0.05f)
                    {
                        p.visual.rotation = Quaternion.LookRotation(p.velocity.normalized, Vector3.up);
                    }
                }
            }

            // Tick Alchemical Wildfire pools on the ground
            for (int i = _firePools.Count - 1; i >= 0; i--)
            {
                WildfirePool pool = _firePools[i];
                pool.remainingTime -= dt;
                pool.tickTimer += dt;

                if (pool.visual != null)
                {
                    float pulse = pool.radius * (1.85f + Mathf.Sin(now * 9f + i) * 0.15f);
                    pool.visual.localScale = new Vector3(pulse, 0.25f, pulse);
                }

                if (pool.tickTimer >= 0.45f)
                {
                    pool.tickTimer = 0f;
                    List<CombatantActor> actors = match.AllCombatants;
                    for (int a = 0; a < actors.Count; a++)
                    {
                        CombatantActor target = actors[a];
                        if (target == null || !target.IsAlive || target.CurrentDropState != DropState.Grounded) continue;
                        float d2 = (new Vector2(target.transform.position.x - pool.position.x, target.transform.position.z - pool.position.z)).sqrMagnitude;
                        if (d2 <= pool.radius * pool.radius)
                        {
                            target.ApplyDamage(12f, pool.owner, "Alchemical Wildfire", false, true, match);
                        }
                    }
                }

                if (pool.remainingTime <= 0f)
                {
                    if (pool.visual != null) Destroy(pool.visual.gameObject);
                    _firePools.RemoveAt(i);
                }
            }
        }

        private void SpawnWildfirePool(CombatantActor owner, float groundY, Vector3 hitPos)
        {
            MedievalAudio.PlayWildfire();
            Vector3 poolPos = new Vector3(hitPos.x, groundY + 0.08f, hitPos.z);

            GameObject go = new GameObject("WildfirePool");
            go.transform.SetParent(_projectileRoot, false);
            go.transform.position = poolPos;

            Material fireMat = MaterialLibrary.GetEmissiveMaterial(
                new Color(0.98f, 0.42f, 0.08f),
                new Color(0.98f, 0.36f, 0.05f),
                2.2f);
            MaterialLibrary.CreatePart("FlameDisc", PrimitiveType.Cylinder, go.transform,
                Vector3.zero, new Vector3(1f, 0.2f, 1f), Quaternion.identity, fireMat, false);

            _firePools.Add(new WildfirePool
            {
                owner = owner,
                visual = go.transform,
                position = poolPos,
                radius = 5.4f,
                remainingTime = 7.5f,
                tickTimer = 0f
            });
        }
    }
}
