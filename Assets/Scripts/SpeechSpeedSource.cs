using UnityEngine;

// Bind only instruction sources: sound effects and distraction sounds keep their original pitch.
public class SpeechSpeedSource : MonoBehaviour
{
    private AudioSource source;
    private float originalPitch = 1f;
    public static void Bind(AudioSource source)
    {
        if (source == null) return;
        foreach (var existing in source.GetComponents<SpeechSpeedSource>())
            if (existing.source == source) return;
        var binding = source.gameObject.AddComponent<SpeechSpeedSource>();
        binding.source = source;
        binding.originalPitch = source.pitch;
        binding.Apply(SpeechPlaybackSpeed.Value);
    }
    private void OnEnable()
    {
        SpeechPlaybackSpeed.Changed += Apply;
        Apply(SpeechPlaybackSpeed.Value);
    }
    private void Apply(float multiplier)
    {
        if (source != null) source.pitch = originalPitch * multiplier;
    }
    private void OnDisable()
    {
        SpeechPlaybackSpeed.Changed -= Apply;
        if (source != null) source.pitch = originalPitch;
    }
}
