using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageQuestionResult
{
    public int questionNumber;
    public string objectId;
    public string correctHand;
    public float timeToCorrectSeconds;
    public int wrongAnswerCount;
    public int replayCount;
}

[Serializable]
public class StageLevelResult
{
    public int schemaVersion = 2;
    public string userId;
    public int attemptNumber;
    public string attemptId;
    public string completedAtUtc;
    public int stageNumber;
    public int levelNumber;
    public float totalAnswerSeconds;
    public List<StageQuestionResult> questions = new();
}

// Shared by Stage1 Level1-8; unfinished attempts never overwrite saved results.
public sealed class StageLevelResultRecorder
{
    private readonly StageLevelResult result;
    private readonly int questionCount;
    private StageQuestionResult current;
    private double startedAt;
    private bool saved;

    public StageLevelResultRecorder(int stageNumber, int levelNumber, int questionCount)
    {
        this.questionCount = questionCount;
        result = new StageLevelResult
        {
            userId = GetOrCreateUserId(),
            attemptId = Guid.NewGuid().ToString("N"),
            stageNumber = stageNumber,
            levelNumber = levelNumber
        };
    }

    public void BeginQuestion(int number, string objectId, string hand, double now)
    {
        if (saved || current != null) return;
        current = new StageQuestionResult
        {
            questionNumber = number,
            objectId = objectId,
            correctHand = hand
        };
        startedAt = now;
    }

    public void RecordWrongAnswer()
    {
        if (current != null) current.wrongAnswerCount++;
    }

    public void RecordReplay()
    {
        if (current != null) current.replayCount++;
    }

    public void RecordCorrect(double now)
    {
        if (current == null || saved) return;
        current.timeToCorrectSeconds = (float)Math.Max(0d, now - startedAt);
        result.totalAnswerSeconds += current.timeToCorrectSeconds;
        result.questions.Add(current);
        current = null;
        if (result.questions.Count != questionCount) return;
        result.completedAtUtc = DateTime.UtcNow.ToString("o");
        EnsureHistoryMigrated(result.stageNumber, result.levelNumber);
        int count = PlayerPrefs.GetInt(HistoryCountKey(result.stageNumber, result.levelNumber), 0);
        result.attemptNumber = count + 1;
        string json = JsonUtility.ToJson(result);
        PlayerPrefs.SetString(AttemptKey(result.stageNumber, result.levelNumber, result.attemptNumber), json);
        PlayerPrefs.SetInt(HistoryCountKey(result.stageNumber, result.levelNumber), result.attemptNumber);
        // Keep the latest-result key for existing result screens and unlock checks.
        PlayerPrefs.SetString(StorageKey(result.stageNumber, result.levelNumber), json);
        PlayerPrefs.Save();
        saved = true;
    }

    public static string StorageKey(int stage, int level)
    {
        return $"ListenAndDo.Stage{stage}.Level{level}.Result";
    }

    public static StageLevelResult Load(int stage, int level)
    {
        string json = PlayerPrefs.GetString(StorageKey(stage, level), "");
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<StageLevelResult>(json);
    }

    private const string UserIdKey = "ListenAndDo.LocalUserId";

    public static string GetOrCreateUserId()
    {
        string id = PlayerPrefs.GetString(UserIdKey, "");
        if (!string.IsNullOrEmpty(id)) return id;
        id = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(UserIdKey, id);
        PlayerPrefs.Save();
        return id;
    }

    private static string HistoryCountKey(int stage, int level)
    {
        return $"ListenAndDo.Stage{stage}.Level{level}.HistoryCount";
    }

    private static string AttemptKey(int stage, int level, int attempt)
    {
        return $"ListenAndDo.Stage{stage}.Level{level}.Attempt{attempt}";
    }

    // The previous format only retained the latest completed attempt.
    // Preserve that available record as history entry 1 without replacing it.
    private static void EnsureHistoryMigrated(int stage, int level)
    {
        string countKey = HistoryCountKey(stage, level);
        if (PlayerPrefs.HasKey(countKey)) return;
        StageLevelResult previous = Load(stage, level);
        int count = 0;
        if (previous != null && previous.questions != null && previous.questions.Count > 0)
        {
            previous.userId = string.IsNullOrEmpty(previous.userId)
                ? GetOrCreateUserId() : previous.userId;
            previous.attemptNumber = 1;
            PlayerPrefs.SetString(AttemptKey(stage, level, 1), JsonUtility.ToJson(previous));
            count = 1;
        }
        PlayerPrefs.SetInt(countKey, count);
        PlayerPrefs.Save();
    }

    public static List<StageLevelResult> LoadHistory(int stage, int level)
    {
        EnsureHistoryMigrated(stage, level);
        int count = PlayerPrefs.GetInt(HistoryCountKey(stage, level), 0);
        List<StageLevelResult> history = new();
        for (int attempt = 1; attempt <= count; attempt++)
        {
            string json = PlayerPrefs.GetString(AttemptKey(stage, level, attempt), "");
            if (string.IsNullOrEmpty(json)) continue;
            StageLevelResult entry = JsonUtility.FromJson<StageLevelResult>(json);
            if (entry != null) history.Add(entry);
        }
        return history;
    }

}
