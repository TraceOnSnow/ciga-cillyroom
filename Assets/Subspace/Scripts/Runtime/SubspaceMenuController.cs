using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Text settingsTitleText;
        [SerializeField] private Text musicLabelText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Text sfxLabelText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private SubspaceAudioController audioController;
        [SerializeField] private SubspaceTextConfig textConfig;

        public void Configure(GameObject rootObject, Image backgroundImage, Text title, Text subtitle, Button button)
        {
            root = rootObject;
            background = backgroundImage;
            titleText = title;
            subtitleText = subtitle;
            startButton = button;
        }

        public void ConfigureButtons(Button settings, Button exit)
        {
            settingsButton = settings;
            exitButton = exit;
        }

        public void ConfigureSettings(GameObject settingsPanel, Text title, Text musicLabel, Slider music, Text sfxLabel, Slider sfx, Button close)
        {
            settingsRoot = settingsPanel;
            settingsTitleText = title;
            musicLabelText = musicLabel;
            musicSlider = music;
            sfxLabelText = sfxLabel;
            sfxSlider = sfx;
            settingsCloseButton = close;
        }

        public void SetAudioController(SubspaceAudioController controller)
        {
            audioController = controller;
            BindAudioControls();
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
            UpdateStaticText();
        }

        public void Show(Sprite backgroundSprite, Color fallbackColor, UnityAction onStart)
        {
            EnsureMenuObjects();
            if (root != null)
            {
                root.SetActive(true);
            }

            if (background != null)
            {
                background.sprite = backgroundSprite;
                background.color = backgroundSprite != null ? Color.white : fallbackColor;
            }

            if (titleText != null)
            {
                titleText.text = TextConfig.menuTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = TextConfig.menuSubtitle;
            }

            if (startButton != null)
            {
                var buttonText = startButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.menuStartButtonText;
                }

                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(onStart);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(ShowSettings);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitGame);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveAllListeners();
                settingsCloseButton.onClick.AddListener(HideSettings);
            }

            BindAudioControls();
            RegisterButtonAudio();
            HideSettings();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void UpdateStaticText()
        {
            EnsureMenuObjects();
            if (titleText != null)
            {
                titleText.text = TextConfig.menuTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = TextConfig.menuSubtitle;
            }

            if (startButton != null)
            {
                var buttonText = startButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.menuStartButtonText;
                }
            }

            SetButtonText(settingsButton, TextConfig.menuSettingsButtonText);
            SetButtonText(exitButton, TextConfig.menuExitButtonText);

            if (settingsTitleText != null)
            {
                settingsTitleText.text = TextConfig.settingsTitleText;
            }

            if (musicLabelText != null)
            {
                musicLabelText.text = TextConfig.musicVolumeText;
            }

            if (sfxLabelText != null)
            {
                sfxLabelText.text = TextConfig.sfxVolumeText;
            }

            SetButtonText(settingsCloseButton, TextConfig.settingsCloseButtonText);
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

            audioController.RegisterButton(startButton);
            audioController.RegisterButton(settingsButton);
            audioController.RegisterButton(exitButton);
            audioController.RegisterButton(settingsCloseButton);
        }

        private void ShowSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(true);
            }
        }

        private void HideSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(false);
            }
        }

        private void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            Debug.Log("[Subspace] Exit Game clicked. Application.Quit is ignored in the Unity Editor.");
#endif
        }

        private void EnsureMenuObjects()
        {
            if (root == null)
            {
                CreateRuntimeMenu();
                return;
            }

            if (settingsButton == null && startButton != null)
            {
                settingsButton = CreateRuntimeButton(root.transform, "Settings Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(240f, 58f));
            }

            if (exitButton == null && startButton != null)
            {
                exitButton = CreateRuntimeButton(root.transform, "Exit Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(240f, 58f));
            }

            if (settingsRoot == null)
            {
                CreateRuntimeSettingsPanel();
            }
        }

        private void CreateRuntimeMenu()
        {
            var parent = GetComponentInParent<Canvas>() != null ? GetComponentInParent<Canvas>().transform : transform;
            var panel = CreateRuntimePanel(parent, "Menu Screen", new Color(0.02f, 0.025f, 0.03f, 0.95f), Vector2.zero, Vector2.zero, Vector2.zero);
            root = panel.gameObject;
            Stretch(panel.rectTransform);

            background = CreateRuntimePanel(root.transform, "Menu Background", Color.clear, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            titleText = CreateRuntimeText(root.transform, "Menu Title", 64, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), new Vector2(620f, 82f));
            subtitleText = CreateRuntimeText(root.transform, "Menu Subtitle", 28, new Color(0.7f, 0.78f, 0.86f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(560f, 44f));
            startButton = CreateRuntimeButton(root.transform, "Start Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(280f, 68f));
            settingsButton = CreateRuntimeButton(root.transform, "Settings Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(240f, 58f));
            exitButton = CreateRuntimeButton(root.transform, "Exit Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -184f), new Vector2(240f, 58f));
            CreateRuntimeSettingsPanel();
        }

        private void CreateRuntimeSettingsPanel()
        {
            var panel = CreateRuntimePanel(root.transform, "Settings Panel", new Color(0.06f, 0.07f, 0.08f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 310f));
            settingsRoot = panel.gameObject;
            settingsTitleText = CreateRuntimeText(settingsRoot.transform, "Settings Title", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 112f), new Vector2(340f, 46f));
            musicLabelText = CreateRuntimeText(settingsRoot.transform, "Music Volume Label", 20, Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(-120f, 42f), new Vector2(130f, 34f));
            musicSlider = CreateRuntimeSlider(settingsRoot.transform, "Music Volume Slider", new Vector2(0.5f, 0.5f), new Vector2(75f, 42f), new Vector2(210f, 22f));
            sfxLabelText = CreateRuntimeText(settingsRoot.transform, "SFX Volume Label", 20, Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(-120f, -22f), new Vector2(130f, 34f));
            sfxSlider = CreateRuntimeSlider(settingsRoot.transform, "SFX Volume Slider", new Vector2(0.5f, 0.5f), new Vector2(75f, -22f), new Vector2(210f, 22f));
            settingsCloseButton = CreateRuntimeButton(settingsRoot.transform, "Settings Close Button", new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(180f, 52f));
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = value;
            }
        }

        private static Button CreateRuntimeButton(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = CreateRuntimePanel(parent, name, new Color(0.16f, 0.42f, 0.55f, 1f), anchor, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<SubspaceButtonAudio>();
            CreateRuntimeText(image.transform, "Label", 24, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return button;
        }

        private static Image CreateRuntimePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
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

        private static Text CreateRuntimeText(Transform parent, string name, int size, Color color, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 rectSize)
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

        private static Slider CreateRuntimeSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rootObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            rootObject.transform.SetParent(parent, false);
            var rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = CreateRuntimePanel(rootObject.transform, "Background", new Color(0.16f, 0.18f, 0.2f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(rootObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            var fill = CreateRuntimePanel(fillArea.transform, "Fill", new Color(0.09f, 0.72f, 0.94f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(size.x - 12f, size.y));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(rootObject.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, -6f);
            handleAreaRect.offsetMax = new Vector2(-8f, 6f);

            var handle = CreateRuntimePanel(handleArea.transform, "Handle", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 34f));

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
