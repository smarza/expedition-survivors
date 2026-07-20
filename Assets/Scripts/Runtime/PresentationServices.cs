using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition
{
    public enum PresentationCue
    {
        Navigate,
        Confirm,
        Back,
        AxeThrow,
        ProjectileTrail,
        Impact,
        EnemyDefeated,
        ExperiencePickup,
        RavenGuard,
        Ultimate,
        LevelUp,
        PlayerDowned,
        PlayerRevived,
        Victory,
        Defeat,
        BossSpawn
    }

    public enum PresentationMusicState
    {
        Menu,
        Expedition,
        Boss,
        Reward,
        Result
    }

    public sealed class PresentationDirector : MonoBehaviour
    {
        private GameDirector _director;
        private CameraFollow _cameraFollow;
        private PresentationAudioMixer _audio;
        private PresentationVfxPool _vfx;
        private PresentationMusicState _musicState;

        public int ActiveVfx => _vfx != null ? _vfx.ActiveCount : 0;
        public int ActiveSfxVoices => _audio != null ? _audio.ActiveVoiceCount : 0;
        public PresentationMusicState MusicState => _musicState;

        public void Initialize(GameDirector director, CameraFollow cameraFollow)
        {
            _director = director;
            _cameraFollow = cameraFollow;
            _audio = gameObject.AddComponent<PresentationAudioMixer>();
            _vfx = gameObject.AddComponent<PresentationVfxPool>();
            SetMusicState(PresentationMusicState.Menu);
        }

        public void AttachRunAmbience(Transform runRoot)
        {
            if (runRoot == null || _director == null)
            {
                return;
            }

            var map = _director.SelectedMap;
            var ambience = new GameObject("Biome Ambience");
            ambience.transform.SetParent(runRoot, false);

            if (map.LandmarkProfileId == BiomeCatalog.CanopyId)
            {
                ambience.AddComponent<CanopyAmbience>().Initialize(_director);
                return;
            }

            if (map.LandmarkProfileId == BiomeCatalog.RelayId)
            {
                ambience.AddComponent<RelayAmbience>().Initialize(_director);
                return;
            }

            ambience.AddComponent<FrostboundAmbience>().Initialize(_director);
        }

        public void Notify(PresentationCue cue, Vector2 position, Color color, float scale = 1f)
        {
            if (cue != PresentationCue.ProjectileTrail) _audio?.Play(cue);
            if (cue == PresentationCue.Navigate || cue == PresentationCue.Confirm ||
                cue == PresentationCue.Back || cue == PresentationCue.LevelUp) return;
            _vfx?.Emit(cue, position, color, scale);
            if (_cameraFollow == null) return;
            switch (cue)
            {
                case PresentationCue.Impact: _cameraFollow.AddTrauma(0.08f); break;
                case PresentationCue.PlayerDowned: _cameraFollow.AddTrauma(0.28f); break;
                case PresentationCue.Ultimate: _cameraFollow.AddTrauma(0.48f); break;
                case PresentationCue.BossSpawn: _cameraFollow.AddTrauma(0.62f); break;
                case PresentationCue.Victory: _cameraFollow.AddTrauma(0.35f); break;
            }
        }

        private void Update()
        {
            if (_director == null) return;
            var next = MusicFor(_director.State, _director.BossSpawned);
            if (next != _musicState) SetMusicState(next);
        }

        private void SetMusicState(PresentationMusicState state)
        {
            _musicState = state;
            _audio?.SetMusicState(state);
        }

        public static PresentationMusicState MusicFor(RunState state, bool bossSpawned)
        {
            if (state == RunState.LevelUp || state == RunState.BuildDetails || state == RunState.Settings)
                return PresentationMusicState.Reward;
            if (state == RunState.Victory || state == RunState.GameOver) return PresentationMusicState.Result;
            if (state == RunState.Playing || state == RunState.Paused)
                return bossSpawned ? PresentationMusicState.Boss : PresentationMusicState.Expedition;
            return PresentationMusicState.Menu;
        }
    }

    public static class PresentationMix
    {
        public static float LinearBusVolume(float master, float bus) =>
            Mathf.Clamp01(master) * Mathf.Clamp01(master) * Mathf.Clamp01(bus);

        public static int Priority(PresentationCue cue)
        {
            switch (cue)
            {
                case PresentationCue.Ultimate:
                case PresentationCue.BossSpawn:
                case PresentationCue.PlayerDowned:
                case PresentationCue.Victory:
                case PresentationCue.Defeat: return 16;
                case PresentationCue.LevelUp:
                case PresentationCue.PlayerRevived:
                case PresentationCue.RavenGuard: return 48;
                case PresentationCue.Impact:
                case PresentationCue.EnemyDefeated: return 96;
                case PresentationCue.AxeThrow:
                case PresentationCue.ProjectileTrail:
                case PresentationCue.ExperiencePickup: return 144;
                default: return 196;
            }
        }
    }

    public sealed class PresentationAudioMixer : MonoBehaviour
    {
        private const int VoiceCount = 10;
        private static readonly string[] MusicResourcePaths =
        {
            "Audio/Music/RavenboundTheme",
            "Audio/Music/FrostboundExpedition",
            "Audio/Music/JotunnOmen",
            "Audio/Music/RewardVerse",
            "Audio/Music/SagaResult"
        };
        private readonly AudioSource[] _voices = new AudioSource[VoiceCount];
        private readonly PresentationCue[] _voiceCues = new PresentationCue[VoiceCount];
        private AudioSource _music;
        private AudioClip[] _musicClips;
        private AudioClip[] _sfxClips;
        private PresentationMusicState _pendingMusic;
        private bool _audioReady;
#if UNITY_WEBGL
        private bool _unlocked;
#else
        private bool _unlocked = true;
#endif

        public int ActiveVoiceCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _voices.Length; i++)
                    if (_voices[i] != null && _voices[i].isPlaying) count++;
                return count;
            }
        }

        private void Awake()
        {
            if (Application.isBatchMode) return;
            _music = CreateSource("Music Bus", true);
            _music.priority = 32;
            _musicClips = new AudioClip[MusicResourcePaths.Length];
            for (var i = 0; i < _musicClips.Length; i++)
                _musicClips[i] = Resources.Load<AudioClip>(MusicResourcePaths[i]);
            _sfxClips = new AudioClip[Enum.GetValues(typeof(PresentationCue)).Length];
            for (var i = 0; i < _sfxClips.Length; i++)
                _sfxClips[i] = Resources.Load<AudioClip>(SfxResourcePath((PresentationCue)i));
            for (var i = 0; i < _voices.Length; i++) _voices[i] = CreateSource($"SFX Voice {i + 1}", false);
            _audioReady = true;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void Update()
        {
            if (!_audioReady || _music == null) return;
            if (!_unlocked) _unlocked = HasInteraction();
            var data = PresentationPreferences.Data;
            _music.volume = PresentationMix.LinearBusVolume(data.MasterVolume, data.MusicVolume);
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voices[i] == null) continue;
                _voices[i].volume = PresentationMix.LinearBusVolume(data.MasterVolume, data.SfxVolume);
            }
            if (_unlocked && !_music.isPlaying)
            {
                _music.clip = _musicClips[(int)_pendingMusic];
                if (_music.clip != null) _music.Play();
            }
        }

        public void SetMusicState(PresentationMusicState state)
        {
            _pendingMusic = state;
            if (!_audioReady || _music == null || !_unlocked) return;
            var clip = _musicClips[(int)state];
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            _music.Stop();
            _music.clip = clip;
            _music.Play();
        }

        public void Play(PresentationCue cue)
        {
            if (!_audioReady) return;
            if (!_unlocked) _unlocked = HasInteraction();
            if (!_unlocked || _voices[0] == null) return;
            var priority = PresentationMix.Priority(cue);
            var selected = -1;
            var weakestPriority = -1;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voices[i] == null) continue;
                if (!_voices[i].isPlaying) { selected = i; break; }
                var activePriority = PresentationMix.Priority(_voiceCues[i]);
                if (activePriority > weakestPriority && priority < activePriority)
                {
                    weakestPriority = activePriority;
                    selected = i;
                }
            }
            if (selected < 0) return;
            if (_sfxClips[(int)cue] == null) return;
            _voiceCues[selected] = cue;
            _voices[selected].priority = priority;
            _voices[selected].clip = _sfxClips[(int)cue];
            _voices[selected].pitch = 1f;
            _voices[selected].Play();
        }

        private static bool HasInteraction()
        {
            if (Keyboard.current != null &&
                (Keyboard.current.anyKey.wasPressedThisFrame || Keyboard.current.anyKey.isPressed))
                return true;

            if (Mouse.current != null &&
                (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.isPressed))
                return true;

            if (Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                 Touchscreen.current.primaryTouch.press.isPressed))
                return true;

            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonSouth.isPressed ||
                    gamepad.buttonNorth.wasPressedThisFrame || gamepad.buttonNorth.isPressed ||
                    gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonEast.isPressed ||
                    gamepad.buttonWest.wasPressedThisFrame || gamepad.buttonWest.isPressed ||
                    gamepad.startButton.wasPressedThisFrame || gamepad.startButton.isPressed ||
                    gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame ||
                    gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame ||
                    gamepad.leftStick.ReadValue().sqrMagnitude > 0.2f)
                    return true;
            }

            return false;
        }

        public static string MusicResourcePath(PresentationMusicState state)
        {
            return MusicResourcePaths[(int)state];
        }

        public static string SfxResourcePath(PresentationCue cue)
        {
            return $"Audio/SFX/{cue}";
        }
    }

    public sealed class PresentationVfxPool : MonoBehaviour
    {
        private ComponentPool<PresentationBurst> _pool;
        public int ActiveCount => _pool?.ActiveCount ?? 0;

        private void Awake()
        {
            var root = new GameObject("Presentation VFX Pool").transform;
            root.SetParent(transform, false);
            _pool = new ComponentPool<PresentationBurst>(() =>
            {
                var item = new GameObject("Pooled Presentation Burst");
                item.transform.SetParent(root, false);
                return item.AddComponent<PresentationBurst>();
            }, root, 96);
        }

        public void Emit(PresentationCue cue, Vector2 position, Color color, float scale)
        {
            if (_pool == null) return;
            _pool.Get(position).Initialize(this, cue, color, scale);
        }

        public void Release(PresentationBurst burst) => _pool.Release(burst);
    }

    public sealed class PresentationBurst : MonoBehaviour, IPoolableComponent
    {
        private PresentationVfxPool _owner;
        private SpriteRenderer _renderer;
        private float _age;
        private float _duration;
        private float _scale;
        private Color _color;
        private PresentationCue _cue;

        private void Awake()
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = RuntimeAssets.Diamond;
            _renderer.sortingOrder = 18;
        }

        public void Initialize(PresentationVfxPool owner, PresentationCue cue, Color color, float scale)
        {
            _owner = owner;
            _cue = cue;
            _color = color;
            _scale = Mathf.Max(0.1f, scale);
            _duration = cue == PresentationCue.Ultimate || cue == PresentationCue.Victory ||
                        cue == PresentationCue.BossSpawn ? 0.7f : 0.24f;
            if (PresentationPreferences.Data.ReducedFlashes) _duration *= 0.72f;
            _age = 0f;
            _renderer.sprite = cue == PresentationCue.Ultimate || cue == PresentationCue.RavenGuard ||
                               cue == PresentationCue.BossSpawn
                ? RuntimeAssets.Circle : RuntimeAssets.Diamond;
            transform.localScale = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(_age / Mathf.Max(0.01f, _duration));
            var eased = 1f - (1f - progress) * (1f - progress);
            var start = _cue == PresentationCue.Impact ? 0.12f : 0.2f;
            var end = _cue == PresentationCue.Ultimate || _cue == PresentationCue.BossSpawn ? 6f : 1.1f;
            transform.localScale = Vector3.one * Mathf.Lerp(start, end, eased) * _scale;
            transform.Rotate(0f, 0f, 420f * Time.unscaledDeltaTime);
            var maximumAlpha = PresentationPreferences.Data.ReducedFlashes ? 0.24f : 0.58f;
            _renderer.color = new Color(_color.r, _color.g, _color.b,
                Mathf.Lerp(maximumAlpha, 0f, progress));
            if (_age >= _duration) _owner.Release(this);
        }

        public void OnReleasedToPool()
        {
            _owner = null;
            _age = 0f;
            transform.localScale = Vector3.zero;
        }
    }

    public sealed class FrostboundAmbience : MonoBehaviour
    {
        private const int FlakeCount = 36;
        private readonly Transform[] _flakes = new Transform[FlakeCount];
        private GameDirector _director;

        public void Initialize(GameDirector director)
        {
            _director = director;
            for (var i = 0; i < _flakes.Length; i++)
            {
                var flake = new GameObject($"Snow {i + 1}");
                flake.transform.SetParent(transform, false);
                var renderer = flake.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeAssets.Diamond;
                renderer.color = new Color(0.62f, 0.88f, 1f, 0.2f + Hash01(i * 17) * 0.25f);
                renderer.sortingOrder = -7;
                flake.transform.localScale = Vector3.one * Mathf.Lerp(0.025f, 0.075f, Hash01(i * 31));
                _flakes[i] = flake.transform;
            }
        }

        private void Update()
        {
            if (_director == null) return;
            var center = _director.GroupCenter;
            transform.position = center;
            var time = Time.unscaledTime;
            for (var i = 0; i < _flakes.Length; i++)
            {
                var speed = 0.35f + Hash01(i * 13) * 0.8f;
                var x = Mathf.Repeat(Hash01(i * 43) * 26f + time * speed * 0.4f, 26f) - 13f;
                var y = 8f - Mathf.Repeat(Hash01(i * 71) * 17f + time * speed, 17f);
                _flakes[i].localPosition = new Vector3(x + Mathf.Sin(time + i) * 0.35f, y, 0f);
                _flakes[i].Rotate(0f, 0f, (20f + i % 5 * 8f) * Time.unscaledDeltaTime);
            }
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                var x = (uint)value + 0x9e3779b9u;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                return (x & 0xffff) / 65535f;
            }
        }
    }

    public sealed class CanopyAmbience : MonoBehaviour
    {
        private const int ParticleCount = 32;
        private readonly Transform[] _particles = new Transform[ParticleCount];
        private GameDirector _director;

        public void Initialize(GameDirector director)
        {
            _director = director;

            for (var i = 0; i < _particles.Length; i++)
            {
                var particle = new GameObject($"Canopy Leaf {i + 1}");
                particle.transform.SetParent(transform, false);
                var renderer = particle.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeAssets.Diamond;
                renderer.color = new Color(0.42f, 0.78f, 0.32f, 0.16f + Hash01(i * 19) * 0.22f);
                renderer.sortingOrder = -7;
                particle.transform.localScale = Vector3.one * Mathf.Lerp(0.02f, 0.06f, Hash01(i * 29));
                _particles[i] = particle.transform;
            }
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            transform.position = _director.GroupCenter;
            var time = Time.unscaledTime;

            for (var i = 0; i < _particles.Length; i++)
            {
                var speed = 0.28f + Hash01(i * 11) * 0.55f;
                var x = Mathf.Repeat(Hash01(i * 37) * 24f + time * speed * 0.35f, 24f) - 12f;
                var y = 7f - Mathf.Repeat(Hash01(i * 53) * 15f + time * speed, 15f);
                _particles[i].localPosition = new Vector3(x + Mathf.Sin(time * 0.8f + i) * 0.25f, y, 0f);
                _particles[i].Rotate(0f, 0f, (16f + i % 4 * 6f) * Time.unscaledDeltaTime);
            }
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                var x = (uint)value + 0x9e3779b9u;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                return (x & 0xffff) / 65535f;
            }
        }
    }

    public sealed class RelayAmbience : MonoBehaviour
    {
        private const int SparkCount = 28;
        private readonly Transform[] _sparks = new Transform[SparkCount];
        private GameDirector _director;

        public void Initialize(GameDirector director)
        {
            _director = director;

            for (var i = 0; i < _sparks.Length; i++)
            {
                var spark = new GameObject($"Relay Spark {i + 1}");
                spark.transform.SetParent(transform, false);
                var renderer = spark.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeAssets.Diamond;
                renderer.color = new Color(0.92f, 0.58f, 0.18f, 0.12f + Hash01(i * 23) * 0.18f);
                renderer.sortingOrder = -7;
                spark.transform.localScale = Vector3.one * Mathf.Lerp(0.02f, 0.05f, Hash01(i * 41));
                _sparks[i] = spark.transform;
            }
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            transform.position = _director.GroupCenter;
            var time = Time.unscaledTime;

            for (var i = 0; i < _sparks.Length; i++)
            {
                var speed = 0.45f + Hash01(i * 17) * 0.75f;
                var x = Mathf.Repeat(Hash01(i * 59) * 22f + time * speed * 0.5f, 22f) - 11f;
                var y = 6f - Mathf.Repeat(Hash01(i * 67) * 13f + time * speed * 1.1f, 13f);
                _sparks[i].localPosition = new Vector3(x, y, 0f);
                _sparks[i].Rotate(0f, 0f, (28f + i % 3 * 10f) * Time.unscaledDeltaTime);
            }
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                var x = (uint)value + 0x9e3779b9u;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                return (x & 0xffff) / 65535f;
            }
        }
    }
}
