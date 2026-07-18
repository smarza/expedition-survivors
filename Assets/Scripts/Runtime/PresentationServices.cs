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
        Defeat
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
            if (runRoot == null) return;
            var ambience = new GameObject("Frostbound Ambience");
            ambience.transform.SetParent(runRoot, false);
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
        private readonly AudioSource[] _voices = new AudioSource[VoiceCount];
        private readonly PresentationCue[] _voiceCues = new PresentationCue[VoiceCount];
        private AudioSource _music;
        private AudioClip[] _musicClips;
        private AudioClip[] _sfxClips;
        private PresentationMusicState _pendingMusic;
        private bool _unlocked;

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
            _musicClips = new[]
            {
                MakeTone("Ravenbound Theme", 110f, 0.16f, 4f, true),
                MakeTone("Frostbound Expedition", 146.8f, 0.12f, 4f, true),
                MakeTone("Jotunn Omen", 82.4f, 0.18f, 4f, true),
                MakeTone("Reward Verse", 196f, 0.1f, 4f, true),
                MakeTone("Saga Result", 130.8f, 0.14f, 4f, true)
            };
            _sfxClips = new AudioClip[Enum.GetValues(typeof(PresentationCue)).Length];
            for (var i = 0; i < _voices.Length; i++) _voices[i] = CreateSource($"SFX Voice {i + 1}", false);
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
            if (_music == null) return;
            if (!_unlocked) _unlocked = HasInteraction();
            var data = PresentationPreferences.Data;
            _music.volume = PresentationMix.LinearBusVolume(data.MasterVolume, data.MusicVolume);
            for (var i = 0; i < _voices.Length; i++)
                _voices[i].volume = PresentationMix.LinearBusVolume(data.MasterVolume, data.SfxVolume);
            if (_unlocked && !_music.isPlaying)
            {
                _music.clip = _musicClips[(int)_pendingMusic];
                _music.Play();
            }
        }

        public void SetMusicState(PresentationMusicState state)
        {
            _pendingMusic = state;
            if (_music == null || !_unlocked) return;
            var clip = _musicClips[(int)state];
            if (_music.clip == clip && _music.isPlaying) return;
            _music.Stop();
            _music.clip = clip;
            _music.Play();
        }

        public void Play(PresentationCue cue)
        {
            if (!_unlocked || _voices[0] == null) return;
            var priority = PresentationMix.Priority(cue);
            var selected = -1;
            var weakestPriority = -1;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) { selected = i; break; }
                var activePriority = PresentationMix.Priority(_voiceCues[i]);
                if (activePriority > weakestPriority && priority < activePriority)
                {
                    weakestPriority = activePriority;
                    selected = i;
                }
            }
            if (selected < 0) return;
            if (_sfxClips[(int)cue] == null) _sfxClips[(int)cue] = MakeCue(cue);
            _voiceCues[selected] = cue;
            _voices[selected].priority = priority;
            _voices[selected].clip = _sfxClips[(int)cue];
            _voices[selected].pitch = 1f;
            _voices[selected].Play();
        }

        private static bool HasInteraction()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.isPressed) return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                if (gamepad.buttonSouth.isPressed || gamepad.startButton.isPressed ||
                    gamepad.leftStick.ReadValue().sqrMagnitude > 0.2f) return true;
            }
            return false;
        }

        private static AudioClip MakeCue(PresentationCue cue)
        {
            var frequency = 180f + (int)cue * 24f;
            var duration = cue == PresentationCue.Ultimate || cue == PresentationCue.Victory ? 0.52f : 0.12f;
            var amplitude = cue == PresentationCue.AxeThrow || cue == PresentationCue.ProjectileTrail ||
                            cue == PresentationCue.ExperiencePickup ? 0.08f : 0.16f;
            return MakeTone(cue.ToString(), frequency, amplitude, duration, false);
        }

        private static AudioClip MakeTone(string clipName, float frequency, float amplitude,
            float duration, bool harmonic)
        {
            const int sampleRate = 11025;
            var samples = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = harmonic ? 0.72f + 0.28f * Mathf.Sin(t * Mathf.PI * 0.5f)
                    : Mathf.Sin(Mathf.PI * Mathf.Clamp01(i / (samples * 0.12f))) *
                      Mathf.Clamp01((samples - i) / (samples * 0.35f));
                var wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                if (harmonic) wave += Mathf.Sin(t * frequency * 1.5f * Mathf.PI * 2f) * 0.32f;
                data[i] = wave * amplitude * envelope;
            }
            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (_musicClips != null)
                for (var i = 0; i < _musicClips.Length; i++)
                    if (_musicClips[i] != null) Destroy(_musicClips[i]);
            if (_sfxClips != null)
                for (var i = 0; i < _sfxClips.Length; i++)
                    if (_sfxClips[i] != null) Destroy(_sfxClips[i]);
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
            _duration = cue == PresentationCue.Ultimate || cue == PresentationCue.Victory ? 0.7f : 0.24f;
            if (PresentationPreferences.Data.ReducedFlashes) _duration *= 0.72f;
            _age = 0f;
            _renderer.sprite = cue == PresentationCue.Ultimate || cue == PresentationCue.RavenGuard
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
            var end = _cue == PresentationCue.Ultimate ? 6f : 1.1f;
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
}
