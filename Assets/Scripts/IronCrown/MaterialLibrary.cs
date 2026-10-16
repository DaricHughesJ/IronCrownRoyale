using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public static class MaterialLibrary
    {
        private static readonly Dictionary<int, Material> _matCache = new Dictionary<int, Material>();
        private static Shader _ironCrownShader;
        private static Material _baseResourceMat;
        private static Shader _transparentShader;
        private static Mesh _cubeMesh;
        private static Mesh _cylinderMesh;
        private static Mesh _sphereMesh;
        private static Mesh _capsuleMesh;

        private static Material CreateNewLitMaterialInstance()
        {
            if (_baseResourceMat == null)
            {
                _baseResourceMat = Resources.Load<Material>("IronCrownBaseMat");
            }
            if (_baseResourceMat != null)
            {
                return new Material(_baseResourceMat);
            }

            if (_ironCrownShader == null)
            {
                _ironCrownShader = Resources.Load<Shader>("IronCrownLit");
                if (_ironCrownShader == null) _ironCrownShader = Shader.Find("Custom/IronCrownLit");
                if (_ironCrownShader == null) _ironCrownShader = Shader.Find("Standard");
            }
            return new Material(_ironCrownShader);
        }

        public static Material GetMaterial(Color color, float metallic = 0.05f, float smoothness = 0.2f)
        {
            int key = HashColor(color, metallic, smoothness, false);
            if (_matCache.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            mat = CreateNewLitMaterialInstance();
            mat.color = color;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.enableInstancing = true;
            _matCache[key] = mat;
            return mat;
        }

        public static Material GetEmissiveMaterial(Color baseColor, Color emissionColor, float intensity = 1.6f)
        {
            int key = HashColor(baseColor, intensity, 0.91f, false);
            if (_matCache.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            mat = CreateNewLitMaterialInstance();
            Color boosted = Color.Lerp(baseColor, emissionColor, 0.55f);
            mat.color = boosted;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", boosted);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", boosted);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor * (intensity * 0.45f));
            }
            mat.enableInstancing = true;
            _matCache[key] = mat;
            return mat;
        }

        public static Material GetTransparentMaterial(Color colorWithAlpha)
        {
            int key = HashColor(colorWithAlpha, colorWithAlpha.a, 0.5f, true);
            if (_matCache.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            if (_transparentShader == null)
            {
                _transparentShader = Shader.Find("Sprites/Default");
            }
            mat = _transparentShader != null ? new Material(_transparentShader) : CreateNewLitMaterialInstance();
            mat.color = colorWithAlpha;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colorWithAlpha);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorWithAlpha);
            mat.renderQueue = 3000;
            _matCache[key] = mat;
            return mat;
        }

        private static int HashColor(Color c, float p1, float p2, bool trans)
        {
            int r = Mathf.RoundToInt(c.r * 100f);
            int g = Mathf.RoundToInt(c.g * 100f);
            int b = Mathf.RoundToInt(c.b * 100f);
            int a = Mathf.RoundToInt(c.a * 100f);
            int m = Mathf.RoundToInt(p1 * 50f);
            int s = Mathf.RoundToInt(p2 * 50f);
            int hash = r * 73856093 ^ g * 19349663 ^ b * 83492791 ^ a * 123457 ^ m * 39916801 ^ s * 999983;
            return trans ? ~hash : hash;
        }

        private static void EnsurePrimitiveMeshes()
        {
            if (_cubeMesh != null) return;
            _cubeMesh = ExtractPrimitiveMesh(PrimitiveType.Cube);
            _cylinderMesh = ExtractPrimitiveMesh(PrimitiveType.Cylinder);
            _sphereMesh = ExtractPrimitiveMesh(PrimitiveType.Sphere);
            _capsuleMesh = ExtractPrimitiveMesh(PrimitiveType.Capsule);
        }

        private static Mesh ExtractPrimitiveMesh(PrimitiveType pt)
        {
            GameObject temp = GameObject.CreatePrimitive(pt);
            Mesh m = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return m;
        }

        public static GameObject CreatePart(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Quaternion localRot,
            Material material,
            bool addCollider = false)
        {
            EnsurePrimitiveMeshes();
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            switch (type)
            {
                case PrimitiveType.Cylinder: mf.sharedMesh = _cylinderMesh; break;
                case PrimitiveType.Sphere: mf.sharedMesh = _sphereMesh; break;
                case PrimitiveType.Capsule: mf.sharedMesh = _capsuleMesh; break;
                default: mf.sharedMesh = _cubeMesh; break;
            }

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            if (addCollider)
            {
                if (type == PrimitiveType.Sphere)
                    go.AddComponent<SphereCollider>();
                else if (type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder)
                    go.AddComponent<CapsuleCollider>();
                else
                    go.AddComponent<BoxCollider>();
            }

            return go;
        }
    }
}
