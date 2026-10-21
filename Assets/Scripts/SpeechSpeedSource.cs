using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Bind only instruction sources: sound effects and distraction sounds keep their original pitch.
public class SpeechSpeedSource : MonoBehaviour
{
    private AudioSource source;
    private float originalPitch = 1f;
    private float originalVolume = 1f;
    public static void Bind(AudioSource source)
    {
        if (source == null) return;
        foreach (var existing in source.GetComponents<SpeechSpeedSource>())
            if (existing.source == source) return;
        var binding = source.gameObject.AddComponent<SpeechSpeedSource>();
        binding.source = source;
        binding.originalPitch = source.pitch;
        binding.originalVolume = source.volume;
        binding.ApplyVolume(SpeechVoiceVolume.Value);
        binding.Apply(SpeechPlaybackSpeed.Value);
    }
    private void OnEnable()
    {
        SpeechPlaybackSpeed.Changed += Apply;
        SpeechVoiceVolume.Changed += ApplyVolume;
        ApplyVolume(SpeechVoiceVolume.Value);
        Apply(SpeechPlaybackSpeed.Value);
    }
    private void Apply(float multiplier)
    {
        if (source != null) source.pitch = originalPitch * multiplier;
    }
    private void ApplyVolume(float volume)
    {
        if (source != null) source.volume = originalVolume * volume;
    }
    private void OnDisable()
    {
        SpeechPlaybackSpeed.Changed -= Apply;
        SpeechVoiceVolume.Changed -= ApplyVolume;
        if (source != null) source.volume = originalVolume;
        if (source != null) source.pitch = originalPitch;
    }
}

// Shared by instruction sources only; effects and distraction sounds are unaffected.
public static class SpeechVoiceVolume
{
    private const string PreferenceKey = "ListenAndDo.VoiceVolume";
    public static event Action<float> Changed;
    public static float Value => Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 0.5f));

    public static void SetValue(float volume)
    {
        PlayerPrefs.SetFloat(PreferenceKey, Mathf.Clamp01(volume));
        Changed?.Invoke(Value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Changed = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= BindTopSlider;
        SceneManager.sceneLoaded += BindTopSlider;
        Application.quitting -= Save;
        Application.quitting += Save;
    }

    private static void Save() { PlayerPrefs.Save(); }

    private static void BindTopSlider(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Top") return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            {
                var row = slider.transform.parent;
                if (row == null || row.name != "Voice" || row.parent == null
                    || row.parent.name != "Setting") continue;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.direction = Slider.Direction.LeftToRight;
                slider.SetValueWithoutNotify(Value);
                slider.onValueChanged.AddListener(SetValue);
                // Save on closing settings, rather than writing to disk on each drag tick.
                var save = slider.gameObject.AddComponent<SpeechVoiceVolumeSave>();
                save.hideFlags = HideFlags.HideInInspector;
            }
    }
}

public class SpeechVoiceVolumeSave : MonoBehaviour
{
    private void OnDisable() { PlayerPrefs.Save(); }
    private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
}
