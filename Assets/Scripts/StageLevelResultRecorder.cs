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
    public int schemaVersion = 1;
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
        PlayerPrefs.SetString(StorageKey(result.stageNumber, result.levelNumber), JsonUtility.ToJson(result));
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
}
