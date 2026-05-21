using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class SceneVisualController : MonoBehaviour
{
    public static event Action<string> SceneChanged;
    public static string CurrentSceneId { get; private set; }

    [System.Serializable]
    public class SpriteEntry
    {
        public string id;
        public Sprite sprite;
    }

    [System.Serializable]
    public class CharacterSpriteEntry
    {
        public string characterId;
        public string poseId;
        public Sprite sprite;
    }

    [System.Serializable]
    public class VisualTarget
    {
        public string id;
        public SpriteRenderer spriteRenderer;
        public Image image;
    }

    [System.Serializable]
    public class SceneCharacterState
    {
        public string slotId;
        public string characterId;
        public string poseId;
        public bool hide;
    }

    [System.Serializable]
    public class SceneCharacterVoiceState
    {
        public string speakerName;
        public string voiceProfileId;
        public bool mute;
    }

    [System.Serializable]
    public class ScenePreset
    {
        public string id;
        public string backgroundId;
        public string backgroundMusicId;
        public bool stopBackgroundMusic;
        public string environmentId;
        public bool stopEnvironment;
        public bool hideAllCharactersFirst = true;
        public SceneCharacterState[] characters;
        public SceneCharacterVoiceState[] characterVoices;

        [HideInInspector] public string internalId;
    }

    [Header("Targets")]
    public VisualTarget backgroundTarget;
    public VisualTarget[] characterSlots;

    [Header("Sprites")]
    public SpriteEntry[] backgrounds;
    public CharacterSpriteEntry[] characterSprites;

    [Header("Scene Presets")]
    public ScenePreset[] scenes;

    [Header("Audio")]
    public DialogueAudioController audioController;
    public bool autoAssignIdForNewScenes = true;

    [Header("Transition")]
    public float transitionDuration = 0.35f;
    public bool useFadeTransition = true;

    private int transitionVersion;

    [YarnCommand("background")]
    public IEnumerator SetBackground(string backgroundId)
    {
        int version = BeginVisualTransition();
        Sprite sprite = FindBackground(backgroundId);
        if (sprite == null)
        {
            Debug.LogWarning("Background tidak ditemukan: " + backgroundId);
            yield break;
        }

        yield return SetSpriteRoutine(backgroundTarget, sprite, true, version);
    }

    [YarnCommand("scene")]
    public IEnumerator SetScene(string sceneId)
    {
        int version = BeginVisualTransition();
        ScenePreset scene = FindScene(sceneId);
        if (scene == null)
        {
            Debug.LogWarning("Scene preset tidak ditemukan: " + sceneId);
            yield break;
        }

        CurrentSceneId = sceneId;
        SceneChanged?.Invoke(sceneId);
        ApplySceneAudio(scene);
        List<IEnumerator> transitions = new List<IEnumerator>();

        if (!string.IsNullOrEmpty(scene.backgroundId))
        {
            Sprite backgroundSprite = FindBackground(scene.backgroundId);
            if (backgroundSprite == null)
            {
                Debug.LogWarning("Background tidak ditemukan: " + scene.backgroundId);
            }
            else
            {
                transitions.Add(SetSpriteRoutine(backgroundTarget, backgroundSprite, true, version));
            }
        }

        if (scene.hideAllCharactersFirst)
        {
            AddHideUnusedCharacterTransitions(transitions, scene.characters, version);
        }

        if (scene.characters != null)
        {
            for (int i = 0; i < scene.characters.Length; i++)
            {
                SceneCharacterState state = scene.characters[i];
                if (state == null || string.IsNullOrEmpty(state.slotId))
                {
                    continue;
                }

                VisualTarget slot = FindCharacterSlot(state.slotId);
                if (slot == null)
                {
                    Debug.LogWarning("Slot character tidak ditemukan: " + state.slotId);
                    continue;
                }

                if (state.hide)
                {
                    transitions.Add(HideRoutine(slot, version));
                    continue;
                }

                Sprite sprite = FindCharacterSprite(state.characterId, state.poseId);
                if (sprite == null)
                {
                    Debug.LogWarning("Sprite character tidak ditemukan: " + state.characterId + " / " + state.poseId);
                    continue;
                }

                transitions.Add(SetSpriteRoutine(slot, sprite, true, version));
            }
        }

        yield return RunParallel(transitions);
    }

    [YarnCommand("character")]
    public IEnumerator SetCharacter(string slotId, string characterId, string poseId)
    {
        int version = BeginVisualTransition();
        VisualTarget slot = FindCharacterSlot(slotId);
        if (slot == null)
        {
            Debug.LogWarning("Slot character tidak ditemukan: " + slotId);
            yield break;
        }

        Sprite sprite = FindCharacterSprite(characterId, poseId);
        if (sprite == null)
        {
            Debug.LogWarning("Sprite character tidak ditemukan: " + characterId + " / " + poseId);
            yield break;
        }

        yield return SetSpriteRoutine(slot, sprite, true, version);
    }

    [YarnCommand("hide_character")]
    public IEnumerator HideCharacter(string slotId)
    {
        int version = BeginVisualTransition();
        VisualTarget slot = FindCharacterSlot(slotId);
        if (slot == null)
        {
            Debug.LogWarning("Slot character tidak ditemukan: " + slotId);
            yield break;
        }

        yield return HideRoutine(slot, version);
    }

    [YarnCommand("hide_all_characters")]
    public IEnumerator HideAllCharacters()
    {
        int version = BeginVisualTransition();
        if (characterSlots == null)
        {
            yield break;
        }

        List<IEnumerator> transitions = new List<IEnumerator>();

        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null && !string.IsNullOrEmpty(characterSlots[i].id))
            {
                transitions.Add(HideRoutine(characterSlots[i], version));
            }
        }

        yield return RunParallel(transitions);
    }

    private int BeginVisualTransition()
    {
        transitionVersion++;
        return transitionVersion;
    }

    private bool IsCurrentTransition(int version)
    {
        return version == transitionVersion;
    }

    private void AddHideUnusedCharacterTransitions(List<IEnumerator> transitions, SceneCharacterState[] visibleStates, int version)
    {
        if (characterSlots == null)
        {
            return;
        }

        for (int i = 0; i < characterSlots.Length; i++)
        {
            VisualTarget slot = characterSlots[i];
            if (slot == null || string.IsNullOrEmpty(slot.id) || SceneHasSlotState(visibleStates, slot.id))
            {
                continue;
            }

            transitions.Add(HideRoutine(slot, version));
        }
    }

    private bool SceneHasSlotState(SceneCharacterState[] states, string slotId)
    {
        if (states == null)
        {
            return false;
        }

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] != null && states[i].slotId == slotId)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator RunParallel(List<IEnumerator> routines)
    {
        if (routines == null || routines.Count == 0)
        {
            yield break;
        }

        int runningCount = routines.Count;

        for (int i = 0; i < routines.Count; i++)
        {
            StartCoroutine(RunAndCountRoutine(routines[i], () => runningCount--));
        }

        while (runningCount > 0)
        {
            yield return null;
        }
    }

    private IEnumerator RunAndCountRoutine(IEnumerator routine, System.Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }

    private void ApplySceneAudio(ScenePreset scene)
    {
        if (audioController == null)
        {
            audioController = FindFirstObjectByType<DialogueAudioController>();
        }

        if (audioController == null || scene == null)
        {
            return;
        }

        if (scene.stopBackgroundMusic)
        {
            audioController.StopBackgroundMusic();
        }
        else if (!string.IsNullOrWhiteSpace(scene.backgroundMusicId))
        {
            audioController.PlaySceneBackgroundMusic(scene.backgroundMusicId);
        }

        if (scene.stopEnvironment)
        {
            audioController.StopEnvironment();
        }
        else if (!string.IsNullOrWhiteSpace(scene.environmentId))
        {
            audioController.PlaySceneEnvironment(scene.environmentId);
        }

        audioController.ClearSceneVoiceOverrides();

        if (scene.characterVoices == null)
        {
            return;
        }

        for (int i = 0; i < scene.characterVoices.Length; i++)
        {
            SceneCharacterVoiceState voiceState = scene.characterVoices[i];
            if (voiceState == null || string.IsNullOrWhiteSpace(voiceState.speakerName))
            {
                continue;
            }

            audioController.SetSpeakerVoiceOverride(voiceState.speakerName, voiceState.voiceProfileId, voiceState.mute);
        }
    }

    private void Awake()
    {
        ResolveAudioController();
    }

    private void OnValidate()
    {
        ResolveAudioController();
        EnsureScenePresetIds();
    }

    private void ResolveAudioController()
    {
        if (audioController == null)
        {
            audioController = FindFirstObjectByType<DialogueAudioController>();
        }
    }

    private void EnsureScenePresetIds()
    {
        if (scenes == null)
        {
            return;
        }

        HashSet<string> usedInternalIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> usedSceneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < scenes.Length; i++)
        {
            ScenePreset scene = scenes[i];
            if (scene == null)
            {
                continue;
            }

            bool isNewOrDuplicated = string.IsNullOrWhiteSpace(scene.internalId) || !usedInternalIds.Add(scene.internalId);
            if (isNewOrDuplicated)
            {
                scene.internalId = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(scene.id))
            {
                scene.id = GenerateUniqueSceneId(usedSceneIds);
                continue;
            }

            if (usedSceneIds.Add(scene.id))
            {
                continue;
            }

            if (autoAssignIdForNewScenes && isNewOrDuplicated)
            {
                scene.id = GenerateUniqueSceneId(usedSceneIds);
                continue;
            }

            scene.id = MakeUniqueSceneId(scene.id, usedSceneIds);
        }
    }

    private string GenerateUniqueSceneId(HashSet<string> usedSceneIds)
    {
        int highestNumericId = -1;

        for (int i = 0; i < scenes.Length; i++)
        {
            ScenePreset scene = scenes[i];
            if (scene == null || string.IsNullOrWhiteSpace(scene.id))
            {
                continue;
            }

            if (int.TryParse(scene.id, out int parsedId))
            {
                highestNumericId = Mathf.Max(highestNumericId, parsedId);
            }
        }

        string numericCandidate = (highestNumericId + 1).ToString();
        if (!usedSceneIds.Contains(numericCandidate))
        {
            usedSceneIds.Add(numericCandidate);
            return numericCandidate;
        }

        return MakeUniqueSceneId("scene", usedSceneIds);
    }

    private string MakeUniqueSceneId(string baseId, HashSet<string> usedSceneIds)
    {
        string trimmedBaseId = string.IsNullOrWhiteSpace(baseId) ? "scene" : baseId.Trim();
        string candidate = trimmedBaseId;
        int suffix = 1;

        while (usedSceneIds.Contains(candidate))
        {
            candidate = trimmedBaseId + "_" + suffix;
            suffix++;
        }

        usedSceneIds.Add(candidate);
        return candidate;
    }

    private Sprite FindBackground(string backgroundId)
    {
        if (backgrounds == null)
        {
            return null;
        }

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] != null && backgrounds[i].id == backgroundId)
            {
                return backgrounds[i].sprite;
            }
        }

        return null;
    }

    private ScenePreset FindScene(string sceneId)
    {
        if (scenes == null)
        {
            return null;
        }

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] != null && scenes[i].id == sceneId)
            {
                return scenes[i];
            }
        }

        return null;
    }

    private Sprite FindCharacterSprite(string characterId, string poseId)
    {
        if (characterSprites == null)
        {
            return null;
        }

        for (int i = 0; i < characterSprites.Length; i++)
        {
            CharacterSpriteEntry entry = characterSprites[i];
            if (entry != null && entry.characterId == characterId && entry.poseId == poseId)
            {
                return entry.sprite;
            }
        }

        return null;
    }

    private VisualTarget FindCharacterSlot(string slotId)
    {
        if (characterSlots == null)
        {
            return null;
        }

        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null && characterSlots[i].id == slotId)
            {
                return characterSlots[i];
            }
        }

        return null;
    }

    private IEnumerator SetSpriteRoutine(VisualTarget target, Sprite sprite, bool visible, int version)
    {
        if (target == null)
        {
            yield break;
        }

        if (!useFadeTransition || transitionDuration <= 0f)
        {
            if (!IsCurrentTransition(version))
            {
                yield break;
            }

            SetSprite(target, sprite);
            SetAlpha(target, visible ? 1f : 0f);
            yield break;
        }

        yield return FadeRoutine(target, 0f, version);
        if (!IsCurrentTransition(version))
        {
            yield break;
        }

        SetSprite(target, sprite);
        yield return FadeRoutine(target, visible ? 1f : 0f, version);
    }

    private IEnumerator HideRoutine(VisualTarget target, int version)
    {
        if (target == null)
        {
            yield break;
        }

        if (!useFadeTransition || transitionDuration <= 0f)
        {
            if (!IsCurrentTransition(version))
            {
                yield break;
            }

            SetAlpha(target, 0f);
            SetSprite(target, null);
            yield break;
        }

        yield return FadeRoutine(target, 0f, version);
        if (!IsCurrentTransition(version))
        {
            yield break;
        }

        SetSprite(target, null);
    }

    private IEnumerator FadeRoutine(VisualTarget target, float targetAlpha, int version)
    {
        float startAlpha = GetAlpha(target);
        float time = 0f;

        while (time < transitionDuration)
        {
            if (!IsCurrentTransition(version))
            {
                yield break;
            }

            SetAlpha(target, Mathf.Lerp(startAlpha, targetAlpha, time / transitionDuration));
            time += Time.deltaTime;
            yield return null;
        }

        if (!IsCurrentTransition(version))
        {
            yield break;
        }

        SetAlpha(target, targetAlpha);
    }

    private void SetSprite(VisualTarget target, Sprite sprite)
    {
        if (target.spriteRenderer != null)
        {
            target.spriteRenderer.sprite = sprite;
        }

        if (target.image != null)
        {
            target.image.sprite = sprite;
            target.image.enabled = sprite != null;
        }
    }

    private float GetAlpha(VisualTarget target)
    {
        if (target.spriteRenderer != null)
        {
            return target.spriteRenderer.color.a;
        }

        if (target.image != null)
        {
            return target.image.color.a;
        }

        return 0f;
    }

    private void SetAlpha(VisualTarget target, float alpha)
    {
        if (target.spriteRenderer != null)
        {
            Color color = target.spriteRenderer.color;
            color.a = alpha;
            target.spriteRenderer.color = color;
        }

        if (target.image != null)
        {
            Color color = target.image.color;
            color.a = alpha;
            target.image.color = color;
            target.image.enabled = alpha > 0f && target.image.sprite != null;
        }
    }
}
