using System;
using UnityEngine;

[Serializable]
public class SpeechSpeedChange
{
    public float seconds;
    public float multiplier;
}

public static class SpeechPlaybackSpeed
{
    public const float Minimum = 0.5f, Maximum = 1.5f;
    private const string PreferenceKey = "ListenAndDo.SpeechSpeed";
    private static bool loaded;
    private static float value = 1f;
    private static StageQuestionResult activeQuestion;
    private static double questionStarted;
    public static event Action<float> Changed;
    public static float Value
    {
        get
        {
            if (!loaded) { value = Normalize(PlayerPrefs.GetFloat(PreferenceKey, 1f)); loaded = true; }
            return value;
        }
    }
    public static float Normalize(float multiplier)
    {
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) return 1f;
        return Mathf.Clamp(Mathf.Round(multiplier * 20f) / 20f, Minimum, Maximum);
    }
    public static void SetValue(float multiplier)
    {
        float next = Normalize(multiplier);
        if (Mathf.Approximately(next, Value)) return;
        value = next;
        PlayerPrefs.SetFloat(PreferenceKey, value);
        PlayerPrefs.Save();
        if (activeQuestion != null)
            activeQuestion.speechSpeedChanges.Add(new SpeechSpeedChange
            {
                seconds = (float)Math.Max(0, Time.realtimeSinceStartupAsDouble - questionStarted),
                multiplier = value
            });
        Changed?.Invoke(value);
    }
    public static void BeginQuestion(StageQuestionResult question, double started)
    {
        activeQuestion = question;
        questionStarted = started;
        question.speechSpeedAtStart = Value;
    }
    public static void EndQuestion(StageQuestionResult question)
    {
        if (ReferenceEquals(activeQuestion, question)) activeQuestion = null;
    }
    public static void CancelQuestion() { activeQuestion = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        loaded = false; value = 1f; activeQuestion = null; Changed = null;
    }
}
