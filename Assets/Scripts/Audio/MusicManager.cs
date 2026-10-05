using System;
using System.Collections;
using UnityEngine;
using CodeForge.Combat;

namespace CodeForge.Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("Audio source playing the unfiltered/clean battle track")]
        [SerializeField] private AudioSource cleanSource;

        [Tooltip("Audio source playing the lowpass filtered planning track")]
        [SerializeField] private AudioSource lowpassSource;

        [Header("Volume & Crossfade Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.8f;

        [Tooltip("Duration of the crossfade in seconds")]
        [SerializeField] private float crossfadeDuration = 1.0f;

        [Tooltip("When true, battle track (clean) is loud; when false, calm track (lowpass) is loud")]
        [SerializeField] private bool isBattleActive = false;

        private Coroutine crossfadeCoroutine;
        private bool isInitialized = false;

        public bool IsBattleActive => isBattleActive;
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                ApplyCurrentVolumesImmediate();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ResolveAudioSources();
        }

        private void OnEnable()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Start()
        {
            ResolveAudioSources();
            InitializeAudioPlayback();

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                CombatManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                HandlePhaseChanged(CombatManager.Instance.currentPhase);
            }
            else
            {
                SetBattleState(false, true);
            }
        }

        private void Update()
        {
            // WebGL browser autoplay policy safeguard:
            // If the browser suspended audio before the first user click/tap,
            // ensure playback starts as soon as user interaction occurs.
            if (isInitialized && cleanSource != null && lowpassSource != null)
            {
                if (!cleanSource.isPlaying && !lowpassSource.isPlaying)
                {
                    if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                    {
                        StartSynchronizedPlayback();
                    }
                }
                else
                {
                    // Drift correction: keep tracks within tight sample sync
                    SyncPlaybackSamples();
                }
            }
        }

        public void ResolveAudioSources()
        {
            var sources = GetComponents<AudioSource>();
            if (sources == null || sources.Length == 0) return;

            foreach (var src in sources)
            {
                if (src == null || src.clip == null) continue;
                string clipName = src.clip.name.ToLower();

                if (cleanSource == null && clipName.Contains("clean"))
                {
                    cleanSource = src;
                }
                else if (lowpassSource == null && (clipName.Contains("lowpass") || clipName.Contains("low_pass")))
                {
                    lowpassSource = src;
                }
            }

            // Fallback: if not identified by name, assign first two sources
            if (cleanSource == null && sources.Length > 0) cleanSource = sources[0];
            if (lowpassSource == null && sources.Length > 1) lowpassSource = sources[1];

            // Configure properties for seamless looping
            if (cleanSource != null)
            {
                cleanSource.loop = true;
                cleanSource.playOnAwake = false;
            }
            if (lowpassSource != null)
            {
                lowpassSource.loop = true;
                lowpassSource.playOnAwake = false;
            }
        }

        private void InitializeAudioPlayback()
        {
            if (isInitialized) return;

            // Initially set lowpass active (planning phase), clean silent
            if (cleanSource != null) cleanSource.volume = 0f;
            if (lowpassSource != null) lowpassSource.volume = masterVolume;

            StartSynchronizedPlayback();
            isInitialized = true;
        }

        private void StartSynchronizedPlayback()
        {
            if (cleanSource == null || lowpassSource == null) return;
            if (cleanSource.clip == null || lowpassSource.clip == null) return;

            // Reset sample position
            cleanSource.timeSamples = 0;
            lowpassSource.timeSamples = 0;

            // Schedule both sources to begin at the exact same audio buffer time
            double scheduledTime = AudioSettings.dspTime + 0.05;
            cleanSource.PlayScheduled(scheduledTime);
            lowpassSource.PlayScheduled(scheduledTime);
        }

        private void SyncPlaybackSamples()
        {
            if (cleanSource == null || lowpassSource == null) return;
            if (!cleanSource.isPlaying || !lowpassSource.isPlaying) return;

            int diff = Mathf.Abs(cleanSource.timeSamples - lowpassSource.timeSamples);
            // If tracks drift by more than 1500 samples (~34ms), resync the follower to the leader
            if (diff > 1500)
            {
                lowpassSource.timeSamples = cleanSource.timeSamples;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            bool battleActive = (phase == GamePhase.Running);
            SetBattleState(battleActive);
        }

        /// <summary>
        /// Crossfades music between lowpass (non-battle) and clean (battle) tracks.
        /// </summary>
        /// <param name="battleActive">True = battle running (Clean), False = calm/planning (Lowpass)</param>
        /// <param name="instant">If true, skips crossfade and applies volume immediately</param>
        public void SetBattleState(bool battleActive, bool instant = false)
        {
            isBattleActive = battleActive;

            float targetCleanVol = battleActive ? masterVolume : 0f;
            float targetLowpassVol = battleActive ? 0f : masterVolume;

            if (instant || crossfadeDuration <= 0f)
            {
                if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
                if (cleanSource != null) cleanSource.volume = targetCleanVol;
                if (lowpassSource != null) lowpassSource.volume = targetLowpassVol;
            }
            else
            {
                if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(targetCleanVol, targetLowpassVol, crossfadeDuration));
            }
        }

        private IEnumerator CrossfadeRoutine(float targetClean, float targetLowpass, float duration)
        {
            float startClean = cleanSource != null ? cleanSource.volume : 0f;
            float startLowpass = lowpassSource != null ? lowpassSource.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth step for natural audio crossfade curve
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (cleanSource != null)
                {
                    cleanSource.volume = Mathf.Lerp(startClean, targetClean, smoothT);
                }
                if (lowpassSource != null)
                {
                    lowpassSource.volume = Mathf.Lerp(startLowpass, targetLowpass, smoothT);
                }

                yield return null;
            }

            if (cleanSource != null) cleanSource.volume = targetClean;
            if (lowpassSource != null) lowpassSource.volume = targetLowpass;
            crossfadeCoroutine = null;
        }

        private void ApplyCurrentVolumesImmediate()
        {
            float targetCleanVol = isBattleActive ? masterVolume : 0f;
            float targetLowpassVol = isBattleActive ? 0f : masterVolume;
            if (cleanSource != null) cleanSource.volume = targetCleanVol;
            if (lowpassSource != null) lowpassSource.volume = targetLowpassVol;
        }
    }
}
