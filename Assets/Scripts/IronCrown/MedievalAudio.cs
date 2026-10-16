using UnityEngine;

namespace IronCrown
{
    public class MedievalAudio : MonoBehaviour
    {
        public static MedievalAudio Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioClip _bowReleaseClip;
        private AudioClip _crossbowSnapClip;
        private AudioClip _meleeSwingClip;
        private AudioClip _hitFleshClip;
        private AudioClip _hitArmorClip;
        private AudioClip _lootPickupClip;
        private AudioClip _potionDrinkClip;
        private AudioClip _warHornClip;
        private AudioClip _wildfireBurstClip;
        private AudioClip _gliderOpenClip;

        private void Awake()
        {
            Instance = this;
            _sfxSource = gameObject.GetComponent<AudioSource>();
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = 0.72f;
            GenerateClips();
        }

        private void GenerateClips()
        {
            int rate = 22050;
            _bowReleaseClip = CreateSynthClip("BowRelease", rate, 0.24f, (t, norm) =>
            {
                float freq = Mathf.Lerp(190f, 92f, norm);
                float stringTwang = Mathf.Sin(t * freq * Mathf.PI * 2f) * Mathf.Exp(-norm * 9f);
                float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-norm * 14f) * 0.45f;
                return (stringTwang + whoosh) * 0.65f;
            });

            _crossbowSnapClip = CreateSynthClip("CrossbowSnap", rate, 0.22f, (t, norm) =>
            {
                float click = Mathf.Sin(t * 620f * Mathf.PI * 2f) * Mathf.Exp(-norm * 28f);
                float thud = Mathf.Sin(t * Mathf.Lerp(150f, 65f, norm) * Mathf.PI * 2f) * Mathf.Exp(-norm * 11f);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-norm * 18f) * 0.4f;
                return (click * 0.55f + thud * 0.75f + noise) * 0.7f;
            });

            _meleeSwingClip = CreateSynthClip("MeleeSwing", rate, 0.20f, (t, norm) =>
            {
                float env = Mathf.Sin(norm * Mathf.PI) * Mathf.Exp(-norm * 4f);
                float ring = Mathf.Sin(t * Mathf.Lerp(420f, 180f, norm) * Mathf.PI * 2f) * 0.25f;
                float air = (Random.value * 2f - 1f) * 0.65f;
                return (ring + air) * env * 0.6f;
            });

            _hitFleshClip = CreateSynthClip("HitFlesh", rate, 0.16f, (t, norm) =>
            {
                float thump = Mathf.Sin(t * Mathf.Lerp(130f, 48f, norm) * Mathf.PI * 2f) * Mathf.Exp(-norm * 14f);
                float crunch = (Random.value * 2f - 1f) * Mathf.Exp(-norm * 20f) * 0.35f;
                return (thump + crunch) * 0.8f;
            });

            _hitArmorClip = CreateSynthClip("HitArmor", rate, 0.25f, (t, norm) =>
            {
                float clang = (Mathf.Sin(t * 880f * Mathf.PI * 2f) + 0.6f * Mathf.Sin(t * 1340f * Mathf.PI * 2f)) * Mathf.Exp(-norm * 12f);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-norm * 22f) * 0.3f;
                return (clang * 0.55f + noise) * 0.7f;
            });

            _lootPickupClip = CreateSynthClip("LootPickup", rate, 0.20f, (t, norm) =>
            {
                float f = norm < 0.45f ? 520f : 780f;
                return Mathf.Sin(t * f * Mathf.PI * 2f) * Mathf.Exp(-norm * 8f) * 0.45f;
            });

            _potionDrinkClip = CreateSynthClip("PotionDrink", rate, 0.42f, (t, norm) =>
            {
                float f = Mathf.Lerp(320f, 660f, norm);
                float chime = Mathf.Sin(t * f * Mathf.PI * 2f) * Mathf.Sin(norm * Mathf.PI);
                return chime * 0.45f;
            });

            _warHornClip = CreateSynthClip("WarHorn", rate, 1.35f, (t, norm) =>
            {
                float env = Mathf.Clamp01(norm * 5f) * Mathf.Exp(-Mathf.Max(0f, norm - 0.35f) * 2.5f);
                float f0 = 110f; // Low A2 war-horn
                float wave = Mathf.Sin(t * f0 * Mathf.PI * 2f)
                           + 0.55f * Mathf.Sin(t * f0 * 2f * Mathf.PI * 2f)
                           + 0.30f * Mathf.Sin(t * f0 * 3f * Mathf.PI * 2f);
                return wave * env * 0.32f;
            });

            _wildfireBurstClip = CreateSynthClip("WildfireBurst", rate, 0.55f, (t, norm) =>
            {
                float glass = Mathf.Sin(t * 1480f * Mathf.PI * 2f) * Mathf.Exp(-norm * 25f) * 0.4f;
                float roar = (Random.value * 2f - 1f) * Mathf.Exp(-norm * 4.5f) * 0.65f;
                return (glass + roar) * 0.7f;
            });

            _gliderOpenClip = CreateSynthClip("GliderOpen", rate, 0.45f, (t, norm) =>
            {
                float snap = Mathf.Sin(norm * Mathf.PI) * (Random.value * 2f - 1f);
                return snap * 0.5f;
            });
        }

        private delegate float SampleGenerator(float timeSeconds, float normalizedTime);

        private AudioClip CreateSynthClip(string clipName, int sampleRate, float duration, SampleGenerator generator)
        {
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = (float)i / sampleCount;
                data[i] = Mathf.Clamp(generator(t, norm), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void PlayBow() => Play(Instance != null ? Instance._bowReleaseClip : null, 0.75f);
        public static void PlayCrossbow() => Play(Instance != null ? Instance._crossbowSnapClip : null, 0.85f);
        public static void PlayMeleeSwing() => Play(Instance != null ? Instance._meleeSwingClip : null, 0.75f);
        public static void PlayHitFlesh() => Play(Instance != null ? Instance._hitFleshClip : null, 0.90f);
        public static void PlayHitArmor() => Play(Instance != null ? Instance._hitArmorClip : null, 0.85f);
        public static void PlayLoot() => Play(Instance != null ? Instance._lootPickupClip : null, 0.70f);
        public static void PlayPotion() => Play(Instance != null ? Instance._potionDrinkClip : null, 0.80f);
        public static void PlayWarHorn() => Play(Instance != null ? Instance._warHornClip : null, 0.85f);
        public static void PlayWildfire() => Play(Instance != null ? Instance._wildfireBurstClip : null, 0.85f);
        public static void PlayGlider() => Play(Instance != null ? Instance._gliderOpenClip : null, 0.80f);

        private static void Play(AudioClip clip, float volumeScale)
        {
            if (Instance == null || Instance._sfxSource == null || clip == null) return;
            Instance._sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
}
