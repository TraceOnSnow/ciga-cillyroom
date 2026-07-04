using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspacePauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Text musicLabelText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Text sfxLabelText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private SubspaceAudioController audioController;
        [SerializeField] private SubspaceTextConfig textConfig;

        public bool IsShowing => root != null && root.activeSelf;

        public void Configure(GameObject rootObject, Text title, Button mainMenu, Button exit, Button resume)
        {
            root = rootObject;
            titleText = title;
            mainMenuButton = mainMenu;
            exitButton = exit;
            resumeButton = resume;
        }

        public void ConfigureSliders(Text musicLabel, Slider music, Text sfxLabel, Slider sfx)
        {
            musicLabelText = musicLabel;
            musicSlider = music;
            sfxLabelText = sfxLabel;
            sfxSlider = sfx;
        }

        public void SetAudioController(SubspaceAudioController controller)
        {
            audioController = controller;
            BindAudioControls();
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
            RefreshText();
        }

        public void Show(UnityAction onMainMenu, UnityAction onExit, UnityAction onResume)
        {
            EnsureObjects();
            RefreshText();

            if (root != null)
            {
                root.SetActive(true);
            }

            SetButtonAction(mainMenuButton, onMainMenu);
            SetButtonAction(exitButton, onExit);
            SetButtonAction(resumeButton, onResume);
            BindAudioControls();
            RegisterButtonAudio();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void RefreshText()
        {
            EnsureObjects();

            if (titleText != null)
            {
                titleText.text = TextConfig.pauseTitleText;
            }

            SetButtonText(mainMenuButton, TextConfig.pauseMainMenuButtonText);
            SetButtonText(exitButton, TextConfig.pauseExitButtonText);
            SetButtonText(resumeButton, TextConfig.pauseResumeButtonText);

            if (musicLabelText != null)
            {
                musicLabelText.text = TextConfig.musicVolumeText;
            }

            if (sfxLabelText != null)
            {
                sfxLabelText.text = TextConfig.sfxVolumeText;
            }
        }

        private void EnsureObjects()
        {
            if (root != null)
            {
                return;
            }

            var parent = transform;
            var panel = CreatePanel(parent, "Pause Options Menu", new Color(0.02f, 0.025f, 0.03f, 0.9f), Vector2.zero, Vector2.zero, Vector2.zero);
            root = panel.gameObject;
            Stretch(panel.rectTransform);

            var content = CreatePanel(root.transform, "Options Panel", new Color(0.09f, 0.1f, 0.12f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 340f));
            titleText = CreateText(content.transform, "Options Title", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 132f), new Vector2(300f, 48f));
            musicLabelText = CreateText(content.transform, "Music Volume Label", 18, Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(-108f, 82f), new Vector2(120f, 30f));
            musicSlider = CreateSlider(content.transform, "Music Volume Slider", new Vector2(0.5f, 0.5f), new Vector2(72f, 82f), new Vector2(190f, 20f));
            sfxLabelText = CreateText(content.transform, "SFX Volume Label", 18, Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(-108f, 42f), new Vector2(120f, 30f));
            sfxSlider = CreateSlider(content.transform, "SFX Volume Slider", new Vector2(0.5f, 0.5f), new Vector2(72f, 42f), new Vector2(190f, 20f));
            resumeButton = CreateButton(content.transform, "Resume Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(240f, 52f), new Color(0.09f, 0.72f, 0.94f, 1f));
            mainMenuButton = CreateButton(content.transform, "Main Menu Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(240f, 52f), new Color(0.16f, 0.42f, 0.55f, 1f));
            exitButton = CreateButton(content.transform, "Exit Game Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -148f), new Vector2(240f, 52f), new Color(0.46f, 0.18f, 0.18f, 1f));
            root.SetActive(false);
        }

        private void BindAudioControls()
        {
            if (audioController == null)
            {
                audioController = SubspaceAudioController.Instance;
            }

            if (audioController == null)
            {
                return;
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(audioController.SetMusicVolume);
                musicSlider.value = audioController.MusicVolume;
                musicSlider.onValueChanged.AddListener(audioController.SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(audioController.SetSfxVolume);
                sfxSlider.value = audioController.SfxVolume;
                sfxSlider.onValueChanged.AddListener(audioController.SetSfxVolume);
            }
        }

        private void RegisterButtonAudio()
        {
            if (audioController == null)
            {
                audioController = SubspaceAudioController.Instance;
            }

            if (audioController == null)
            {
                return;
            }

            audioController.RegisterButton(mainMenuButton);
            audioController.RegisterButton(exitButton);
            audioController.RegisterButton(resumeButton);
        }

        private static void SetButtonAction(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var image = CreatePanel(parent, name, color, anchor, position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<SubspaceButtonAudio>();
            CreateText(image.transform, "Label", 24, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return button;
        }

        private static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            var rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            rootObject.transform.SetParent(parent, false);
            var rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = CreatePanel(rootObject.transform, "Background", new Color(0.16f, 0.18f, 0.2f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(rootObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            var fill = CreatePanel(fillArea.transform, "Fill", new Color(0.09f, 0.72f, 0.94f, 1f), Vector2.zero, Vector2.zero, size);
            Stretch(fill.rectTransform);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(rootObject.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, -6f);
            handleAreaRect.offsetMax = new Vector2(-8f, 6f);

            var handle = CreatePanel(handleArea.transform, "Handle", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 32f));

            var slider = rootObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            background.raycastTarget = true;
            return slider;
        }

        private static Text CreateText(Transform parent, string name, int size, Color color, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 rectSize)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, size);
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
