using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class DialogueAudioController : DialoguePresenterBase
{
    [System.Serializable]
    public class AudioEntry
    {
        public string id;
        public AudioClip clip;
    }

    [System.Serializable]
    public class CharacterVoiceProfile
    {
        public string characterName;
        public AudioSource audioSource;
        public AudioClip blipClip;
        public float volume = 0.45f;
        public float pitch = 1f;
        public float pitchRandomRange = 0.04f;
        public float blipInterval = 0.055f;
        public float generatedToneFrequency = 720f;
        public float generatedToneDuration = 0.035f;
    }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource environmentSource;
    public AudioSource fallbackVoiceSource;

    [Header("Audio Libraries")]
    public AudioEntry[] backgroundMusic;
    public AudioEntry[] environmentSounds;

    [Header("Character Voices")]
    public CharacterVoiceProfile[] characterVoices;
    public CharacterVoiceProfile defaultVoice;

    [Header("Fades")]
    public float musicFadeDuration = 0.75f;
    public float environmentFadeDuration = 0.5f;

    private Coroutine musicFadeRoutine;
    private Coroutine environmentFadeRoutine;
    private Coroutine voiceRoutine;
    private AudioClip generatedToneClip;

    [YarnCommand("bgm")]
    public void PlayBackgroundMusic(string musicId)
    {
        AudioClip clip = FindClip(backgroundMusic, musicId);
        if (clip == null)
        {
            Debug.LogWarning("Background music tidak ditemukan: " + musicId);
            return;
        }

        PlayLoopingAudio(musicSource, clip, musicFadeDuration, ref musicFadeRoutine);
    }

    [YarnCommand("stop_bgm")]
    public void StopBackgroundMusic()
    {
        StopLoopingAudio(musicSource, musicFadeDuration, ref musicFadeRoutine);
    }

    [YarnCommand("env")]
    public void PlayEnvironment(string environmentId)
    {
        AudioClip clip = FindClip(environmentSounds, environmentId);
        if (clip == null)
        {
            Debug.LogWarning("Environment sound tidak ditemukan: " + environmentId);
            return;
        }

        PlayLoopingAudio(environmentSource, clip, environmentFadeDuration, ref environmentFadeRoutine);
    }

    [YarnCommand("stop_env")]
    public void StopEnvironment()
    {
        StopLoopingAudio(environmentSource, environmentFadeDuration, ref environmentFadeRoutine);
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        StopVoice();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speakerName = line.CharacterName;

        if (!string.IsNullOrEmpty(speakerName))
        {
            StartVoice(speakerName);
        }

        await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
        StopVoice();
    }

    private void PlayLoopingAudio(AudioSource source, AudioClip clip, float fadeDuration, ref Coroutine fadeRoutine)
    {
        if (source == null)
        {
            Debug.LogWarning("AudioSource belum diisi.");
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(SwitchLoopingAudioRoutine(source, clip, fadeDuration));
    }

    private void StopLoopingAudio(AudioSource source, float fadeDuration, ref Coroutine fadeRoutine)
    {
        if (source == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(StopLoopingAudioRoutine(source, fadeDuration));
    }

    private IEnumerator SwitchLoopingAudioRoutine(AudioSource source, AudioClip clip, float fadeDuration)
    {
        float targetVolume = source.volume > 0f ? source.volume : 1f;

        if (source.isPlaying && fadeDuration > 0f)
        {
            yield return FadeAudioRoutine(source, source.volume, 0f, fadeDuration);
        }

        source.clip = clip;
        source.loop = true;
        source.volume = fadeDuration > 0f ? 0f : targetVolume;
        source.Play();

        if (fadeDuration > 0f)
        {
            yield return FadeAudioRoutine(source, 0f, targetVolume, fadeDuration);
        }
    }

    private IEnumerator StopLoopingAudioRoutine(AudioSource source, float fadeDuration)
    {
        if (fadeDuration > 0f)
        {
            yield return FadeAudioRoutine(source, source.volume, 0f, fadeDuration);
        }

        source.Stop();
        source.clip = null;
    }

    private IEnumerator FadeAudioRoutine(AudioSource source, float startVolume, float targetVolume, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        source.volume = targetVolume;
    }

    private void StartVoice(string speakerName)
    {
        CharacterVoiceProfile profile = FindVoiceProfile(speakerName);
        if (profile == null)
        {
            return;
        }

        StopVoice();
        voiceRoutine = StartCoroutine(VoiceRoutine(profile));
    }

    private void StopVoice()
    {
        if (voiceRoutine != null)
        {
            StopCoroutine(voiceRoutine);
            voiceRoutine = null;
        }
    }

    private IEnumerator VoiceRoutine(CharacterVoiceProfile profile)
    {
        AudioSource source = profile.audioSource != null ? profile.audioSource : fallbackVoiceSource;

        if (source == null)
        {
            Debug.LogWarning("AudioSource suara karakter belum diisi.");
            yield break;
        }

        while (true)
        {
            source.pitch = profile.pitch + Random.Range(-profile.pitchRandomRange, profile.pitchRandomRange);
            source.volume = profile.volume;

            AudioClip clip = profile.blipClip != null ? profile.blipClip : GetGeneratedTone(profile);
            source.PlayOneShot(clip, profile.volume);

            yield return new WaitForSeconds(Mathf.Max(0.01f, profile.blipInterval));
        }
    }

    private AudioClip GetGeneratedTone(CharacterVoiceProfile profile)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * profile.generatedToneDuration));

        if (generatedToneClip == null || generatedToneClip.samples != sampleCount)
        {
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                data[i] = Mathf.Sin(2f * Mathf.PI * profile.generatedToneFrequency * t) * envelope;
            }

            generatedToneClip = AudioClip.Create("Generated Dialogue Blip", sampleCount, 1, sampleRate, false);
            generatedToneClip.SetData(data, 0);
        }

        return generatedToneClip;
    }

    private CharacterVoiceProfile FindVoiceProfile(string speakerName)
    {
        if (characterVoices != null)
        {
            for (int i = 0; i < characterVoices.Length; i++)
            {
                CharacterVoiceProfile profile = characterVoices[i];
                if (profile != null && profile.characterName == speakerName)
                {
                    return profile;
                }
            }
        }

        return defaultVoice;
    }

    private AudioClip FindClip(AudioEntry[] entries, string id)
    {
        if (entries == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].id == id)
            {
                return entries[i].clip;
            }
        }

        return null;
    }
}
