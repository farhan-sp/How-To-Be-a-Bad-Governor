using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class SceneTextEntry
{
    public string visualSceneId;
    [TextArea(2, 6)]
    public string text;
}

public class SceneTypewriterText : MonoBehaviour
{
    [Header("Target")]
    public Text targetText;
    public TMP_Text targetTmpText;
    [TextArea(2, 6)]
    public string fullText;

    [Header("Per Scene Text")]
    public SceneTextEntry[] sceneTexts;

    [Header("End Scene Return")]
    public Button returnToMenuButton;
    public string[] returnButtonVisualSceneIds;
    public int mainMenuSceneIndex = 0;

    [Header("Typing")]
    [Min(0.001f)]
    public float characterDelay = 0.04f;
    public float startDelay = 0f;
    public bool useUnscaledTime = false;
    public bool clearTextBeforeTyping = true;
    public bool preserveTextFromTargetOnAwake = true;

    [Header("Scene Trigger")]
    public bool playOnStartIfSceneMatches = true;
    public bool replayWhenSceneLoads = true;
    public string targetSceneName;
    public int targetSceneBuildIndex = -1;

    private Coroutine typingRoutine;
    private string activeText;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }

        if (targetTmpText == null)
        {
            targetTmpText = GetComponent<TMP_Text>();
        }

        if (preserveTextFromTargetOnAwake && string.IsNullOrEmpty(fullText))
        {
            fullText = GetCurrentText();
        }

        activeText = fullText;
        SetReturnButtonVisible(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneVisualController.SceneChanged += OnVisualSceneChanged;

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void Start()
    {
        if (playOnStartIfSceneMatches && IsTargetScene(SceneManager.GetActiveScene()))
        {
            Play();
        }
        else if (clearTextBeforeTyping)
        {
            SetDisplayedText(string.Empty);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneVisualController.SceneChanged -= OnVisualSceneChanged;

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!replayWhenSceneLoads || !IsTargetScene(scene))
        {
            return;
        }

        Play(scene);
    }

    private void OnVisualSceneChanged(string sceneId)
    {
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            return;
        }

        Play(sceneId);
    }

    public void Play()
    {
        Play(SceneManager.GetActiveScene());
    }

    public void Play(Scene scene)
    {
        if (!HasTargetText())
        {
            Debug.LogWarning("SceneTypewriterText membutuhkan komponen UI Text atau TMP Text.");
            return;
        }

        SetReturnButtonVisible(false);
        activeText = GetTextForScene(scene);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeRoutine());
    }

    public void Play(string visualSceneId)
    {
        if (!HasTargetText())
        {
            Debug.LogWarning("SceneTypewriterText membutuhkan komponen UI Text atau TMP Text.");
            return;
        }

        SetReturnButtonVisible(false);
        activeText = GetTextForVisualScene(visualSceneId);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeRoutine());
    }

    public void ShowInstantly()
    {
        if (!HasTargetText())
        {
            return;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        activeText = GetTextForScene(SceneManager.GetActiveScene());
        SetDisplayedText(activeText);

        if (ShouldShowReturnButton(SceneVisualController.CurrentSceneId))
        {
            SetReturnButtonVisible(true);
        }
    }

    private IEnumerator TypeRoutine()
    {
        if (clearTextBeforeTyping)
        {
            SetDisplayedText(string.Empty);
        }

        if (startDelay > 0f)
        {
            yield return Wait(startDelay);
        }

        for (int i = 0; i < activeText.Length; i++)
        {
            int visibleLength = i + 1;

            if (activeText[i] == '<')
            {
                visibleLength = FindTagEnd(i) + 1;
                i = visibleLength - 1;
            }

            SetDisplayedText(activeText.Substring(0, visibleLength));
            yield return Wait(characterDelay);
        }

        SetDisplayedText(activeText);
        typingRoutine = null;

        if (ShouldShowReturnButton(SceneVisualController.CurrentSceneId))
        {
            SetReturnButtonVisible(true);
        }
    }

    private int FindTagEnd(int startIndex)
    {
        for (int i = startIndex; i < activeText.Length; i++)
        {
            if (activeText[i] == '>')
            {
                return i;
            }
        }

        return startIndex;
    }

    private object Wait(float duration)
    {
        if (useUnscaledTime)
        {
            return new WaitForSecondsRealtime(duration);
        }

        return new WaitForSeconds(duration);
    }

    private bool HasTargetText()
    {
        return targetText != null || targetTmpText != null;
    }

    private string GetCurrentText()
    {
        if (targetText != null)
        {
            return targetText.text;
        }

        if (targetTmpText != null)
        {
            return targetTmpText.text;
        }

        return string.Empty;
    }

    private void SetDisplayedText(string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
        }

        if (targetTmpText != null)
        {
            targetTmpText.text = value;
        }
    }

    private bool IsTargetScene(Scene scene)
    {
        bool hasSceneName = !string.IsNullOrWhiteSpace(targetSceneName);
        bool hasSceneIndex = targetSceneBuildIndex >= 0;

        if (!hasSceneName && !hasSceneIndex)
        {
            return true;
        }

        if (hasSceneName && string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (hasSceneIndex && scene.buildIndex == targetSceneBuildIndex)
        {
            return true;
        }

        return false;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadSceneAsync(mainMenuSceneIndex);
    }

    private string GetTextForScene(Scene scene)
    {
        string currentVisualSceneId = SceneVisualController.CurrentSceneId;
        if (!string.IsNullOrWhiteSpace(currentVisualSceneId))
        {
            return GetTextForVisualScene(currentVisualSceneId);
        }

        return fullText ?? string.Empty;
    }

    private string GetTextForVisualScene(string visualSceneId)
    {
        if (sceneTexts != null)
        {
            for (int i = 0; i < sceneTexts.Length; i++)
            {
                SceneTextEntry entry = sceneTexts[i];

                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.visualSceneId)
                    && string.Equals(entry.visualSceneId, visualSceneId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry.text ?? string.Empty;
                }
            }
        }

        return fullText ?? string.Empty;
    }

    private bool ShouldShowReturnButton(string visualSceneId)
    {
        if (string.IsNullOrWhiteSpace(visualSceneId) || returnButtonVisualSceneIds == null)
        {
            return false;
        }

        for (int i = 0; i < returnButtonVisualSceneIds.Length; i++)
        {
            string candidate = returnButtonVisualSceneIds[i];
            if (!string.IsNullOrWhiteSpace(candidate)
                && string.Equals(candidate, visualSceneId, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void SetReturnButtonVisible(bool visible)
    {
        if (returnToMenuButton == null)
        {
            return;
        }

        returnToMenuButton.gameObject.SetActive(visible);
    }
}
