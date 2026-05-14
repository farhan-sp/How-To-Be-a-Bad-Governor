using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Management")]
    public int gameplaySceneIndex = 1;

    [Header("Menu UI")]
    public CanvasGroup mainMenuGroup;
    public CanvasGroup settingsGroup;
    public Button startButton;
    public Button settingsButton;
    public Button quitButton;
    public Button closeSettingsButton;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Fade")]
    public Image fadeOverlay;
    public float fadeDuration = 0.75f;

    [Header("Auto Build")]
    public bool buildMenuIfMissing = true;

    private bool isStarting;

    private void Awake()
    {
        if (buildMenuIfMissing && mainMenuGroup == null)
        {
            BuildDefaultMenu();
        }

        if (fadeOverlay != null)
        {
            SetFadeAlpha(0f);
            fadeOverlay.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        AddButtonListeners();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }

        ShowMainMenu();
    }

    public void StartGame()
    {
        if (isStarting)
        {
            return;
        }

        StartCoroutine(StartGameRoutine());
    }

    public void ShowSettings()
    {
        SetCanvasGroupVisible(mainMenuGroup, false);
        SetCanvasGroupVisible(settingsGroup, true);
    }

    public void ShowMainMenu()
    {
        SetCanvasGroupVisible(mainMenuGroup, true);
        SetCanvasGroupVisible(settingsGroup, false);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit dipanggil. Di build game, aplikasi akan ditutup.");
#else
        Application.Quit();
#endif
    }

    private IEnumerator StartGameRoutine()
    {
        isStarting = true;
        SetMenuInteractable(false);

        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = true;
            yield return FadeRoutine(1f);
        }

        SetCanvasGroupVisible(settingsGroup, false);
        SetCanvasGroupVisible(mainMenuGroup, false);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(gameplaySceneIndex);
        if (loadOperation == null)
        {
            Debug.LogWarning("Gagal memuat scene index " + gameplaySceneIndex + ".");
            isStarting = false;
            SetMenuInteractable(true);
            if (fadeOverlay != null)
            {
                fadeOverlay.raycastTarget = false;
            }
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = false;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = fadeOverlay.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration));
            time += Time.deltaTime;
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
    }

    private void AddButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ShowSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(ShowMainMenu);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void RemoveButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ShowSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(ShowMainMenu);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }
    }

    private void SetMenuInteractable(bool interactable)
    {
        SetCanvasGroupInteractable(mainMenuGroup, interactable);
        SetCanvasGroupInteractable(settingsGroup, interactable);
    }

    private void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void SetCanvasGroupInteractable(CanvasGroup group, bool interactable)
    {
        if (group == null)
        {
            return;
        }

        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    private void BuildDefaultMenu()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Main Menu Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Image background = CreateImage("Background", canvasRect, new Color(0.06f, 0.07f, 0.08f, 1f));
        StretchToParent(background.rectTransform);

        GameObject menuObject = CreatePanel("Main Menu", canvasRect, new Vector2(420f, 360f));
        mainMenuGroup = menuObject.GetComponent<CanvasGroup>();

        VerticalLayoutGroup menuLayout = menuObject.AddComponent<VerticalLayoutGroup>();
        menuLayout.childAlignment = TextAnchor.MiddleCenter;
        menuLayout.spacing = 18f;
        menuLayout.padding = new RectOffset(32, 32, 32, 32);
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = false;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;

        Text title = CreateText("Title", menuObject.transform, "Main Menu", 44, FontStyle.Bold, TextAnchor.MiddleCenter);
        title.color = Color.white;
        title.GetComponent<LayoutElement>().preferredHeight = 72f;

        startButton = CreateButton("Mulai Button", menuObject.transform, "Mulai");
        settingsButton = CreateButton("Pengaturan Button", menuObject.transform, "Pengaturan");
        quitButton = CreateButton("Keluar Button", menuObject.transform, "Keluar");

        GameObject settingsObject = CreatePanel("Settings Menu", canvasRect, new Vector2(500f, 360f));
        settingsGroup = settingsObject.GetComponent<CanvasGroup>();

        VerticalLayoutGroup settingsLayout = settingsObject.AddComponent<VerticalLayoutGroup>();
        settingsLayout.childAlignment = TextAnchor.MiddleCenter;
        settingsLayout.spacing = 18f;
        settingsLayout.padding = new RectOffset(32, 32, 32, 32);
        settingsLayout.childControlWidth = true;
        settingsLayout.childControlHeight = false;
        settingsLayout.childForceExpandWidth = true;
        settingsLayout.childForceExpandHeight = false;

        Text settingsTitle = CreateText("Settings Title", settingsObject.transform, "Pengaturan", 36, FontStyle.Bold, TextAnchor.MiddleCenter);
        settingsTitle.color = Color.white;
        settingsTitle.GetComponent<LayoutElement>().preferredHeight = 58f;

        CreateText("Volume Label", settingsObject.transform, "Volume", 22, FontStyle.Normal, TextAnchor.MiddleLeft).color = Color.white;
        volumeSlider = CreateSlider("Volume Slider", settingsObject.transform);
        fullscreenToggle = CreateToggle("Fullscreen Toggle", settingsObject.transform, "Fullscreen");
        closeSettingsButton = CreateButton("Kembali Button", settingsObject.transform, "Kembali");

        fadeOverlay = CreateImage("Fade Overlay", canvasRect, Color.black);
        StretchToParent(fadeOverlay.rectTransform);
        fadeOverlay.transform.SetAsLastSibling();
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.12f, 0.13f, 0.15f, 0.94f);

        panel.AddComponent<CanvasGroup>();
        return panel;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 58f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.88f, 0.88f, 0.82f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.94f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.68f, 1f);
        button.colors = colors;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 58f;

        Text text = CreateText("Text", buttonObject.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = new Color(0.08f, 0.09f, 0.1f, 1f);
        StretchToParent(text.rectTransform);

        return button;
    }

    private Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 42f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;

        Image background = CreateImage("Background", sliderObject.transform, new Color(0.22f, 0.23f, 0.25f, 1f));
        StretchToParent(background.rectTransform);

        RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform);
        StretchToParent(fillArea);
        fillArea.offsetMin = new Vector2(8f, 14f);
        fillArea.offsetMax = new Vector2(-8f, -14f);

        Image fill = CreateImage("Fill", fillArea, new Color(0.76f, 0.76f, 0.62f, 1f));
        StretchToParent(fill.rectTransform);

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderObject.transform);
        StretchToParent(handleArea);
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        Image handle = CreateImage("Handle", handleArea, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(24f, 32f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;

        LayoutElement layout = sliderObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;

        return slider;
    }

    private Toggle CreateToggle(string name, Transform parent, string label)
    {
        GameObject toggleObject = new GameObject(name, typeof(RectTransform));
        toggleObject.transform.SetParent(parent, false);

        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 42f);

        Toggle toggle = toggleObject.AddComponent<Toggle>();
        LayoutElement layout = toggleObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;

        Image box = CreateImage("Box", toggleObject.transform, new Color(0.22f, 0.23f, 0.25f, 1f));
        box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        box.rectTransform.pivot = new Vector2(0f, 0.5f);
        box.rectTransform.anchoredPosition = Vector2.zero;
        box.rectTransform.sizeDelta = new Vector2(34f, 34f);

        Image check = CreateImage("Checkmark", box.transform, new Color(0.76f, 0.76f, 0.62f, 1f));
        check.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        check.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        check.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        check.rectTransform.sizeDelta = new Vector2(20f, 20f);

        Text text = CreateText("Label", toggleObject.transform, label, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
        text.color = Color.white;
        text.rectTransform.anchorMin = new Vector2(0f, 0f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.offsetMin = new Vector2(48f, 0f);
        text.rectTransform.offsetMax = Vector2.zero;

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = Screen.fullScreen;
        return toggle;
    }

    private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = GetDefaultFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        textObject.AddComponent<LayoutElement>();
        return text;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
