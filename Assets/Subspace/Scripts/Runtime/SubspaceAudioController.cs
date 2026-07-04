using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Subspace
{
    public sealed class SubspaceAudioController : MonoBehaviour
    {
        public static SubspaceAudioController Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.8f;

        [Header("UI SFX")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip confirmClip;

        [Header("Game SFX")]
        [SerializeField] private AudioClip attackClickClip;
        [SerializeField] private AudioClip victoryEscapeClip;
        [SerializeField] private AudioClip monsterDeathClip;
        [SerializeField] private AudioClip playerDeathClip;
        [SerializeField] private AudioClip rewardChoiceAppearClip;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        public static SubspaceAudioController GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindObjectOfType<SubspaceAudioController>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsureSources();
                existing.AutoAssignMissingClipsInEditor();
                existing.ApplyVolumes();
                return existing;
            }

            var audioManager = GameObject.Find("AudioManager");
            if (audioManager == null)
            {
                audioManager = new GameObject("AudioManager");
            }

            var controller = audioManager.GetComponent<SubspaceAudioController>();
            if (controller == null)
            {
                controller = audioManager.AddComponent<SubspaceAudioController>();
            }

            Instance = controller;
            controller.EnsureSources();
            controller.AutoAssignMissingClipsInEditor();
            controller.ApplyVolumes();
            return controller;
        }

        public void Configure(
            AudioSource music,
            AudioSource sfx,
            AudioClip background,
            AudioClip hover,
            AudioClip confirm,
            AudioClip attackClick,
            AudioClip victoryEscape,
            AudioClip monsterDeath,
            AudioClip playerDeath,
            AudioClip rewardChoiceAppear)
        {
            musicSource = music;
            sfxSource = sfx;
            backgroundMusic = background;
            hoverClip = hover;
            confirmClip = confirm;
            attackClickClip = attackClick;
            victoryEscapeClip = victoryEscape;
            monsterDeathClip = monsterDeath;
            playerDeathClip = playerDeath;
            rewardChoiceAppearClip = rewardChoiceAppear;
            ApplyVolumes();
        }

        public void RegisterButton(Button button)
        {
            RegisterButton(button, true, true);
        }

        public void RegisterButton(Button button, bool playHoverSound, bool playConfirmSound)
        {
            if (button == null)
            {
                return;
            }

            var audio = button.GetComponent<SubspaceButtonAudio>();
            if (audio == null)
            {
                audio = button.gameObject.AddComponent<SubspaceButtonAudio>();
            }

            audio.Configure(this);
            audio.SetPlayback(playHoverSound, playConfirmSound);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        public void PlayBackgroundMusic()
        {
            EnsureSources();
            if (musicSource == null || backgroundMusic == null)
            {
                return;
            }

            if (musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
            }

            musicSource.loop = true;
            musicSource.volume = musicVolume;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void PlayHover() => PlayOneShot(hoverClip);
        public void PlayConfirm() => PlayOneShot(confirmClip);
        public void PlayAttackClick() => PlayOneShot(attackClickClip);
        public void PlayVictoryEscape() => PlayOneShot(victoryEscapeClip);
        public void PlayMonsterDeath() => PlayOneShot(monsterDeathClip);
        public void PlayPlayerDeath() => PlayOneShot(playerDeathClip);
        public void PlayRewardChoiceAppear() => PlayOneShot(rewardChoiceAppearClip);

        private void Awake()
        {
            Instance = this;
            gameObject.name = "AudioManager";
            EnsureSources();
            AutoAssignMissingClipsInEditor();
            ApplyVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            EnsureSources();
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            musicSource.spatialBlend = 0f;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            sfxSource.spatialBlend = 0f;
        }

        private void ApplyVolumes()
        {
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }

        private void AutoAssignMissingClipsInEditor()
        {
#if UNITY_EDITOR
            backgroundMusic = backgroundMusic != null ? backgroundMusic : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/Music/\u6e38\u620f\u80cc\u666f\u97f3\u4e50.m4a");
            hoverClip = hoverClip != null ? hoverClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u9009\u62e9\u97f3\u6548.mp3");
            confirmClip = confirmClip != null ? confirmClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u786e\u5b9a\u97f3\u6548.mp3");
            attackClickClip = attackClickClip != null ? attackClickClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u70b9\u51fb\u653b\u51fb\u97f3\u6548.mp3");
            victoryEscapeClip = victoryEscapeClip != null ? victoryEscapeClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u73a9\u5bb6\u80dc\u5229\u9003\u8131\u97f3\u6548.mp3");
            monsterDeathClip = monsterDeathClip != null ? monsterDeathClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u602a\u7269\u6b7b\u4ea1\u97f3\u6548.mp3");
            playerDeathClip = playerDeathClip != null ? playerDeathClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/\u98de\u8239\u7206\u70b8\u97f3\u6548.mp3");
            rewardChoiceAppearClip = rewardChoiceAppearClip != null ? rewardChoiceAppearClip : AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX/3\u90091\u51fa\u73b0\u97f3\u6548.mp3");
#endif
        }
    }
}
