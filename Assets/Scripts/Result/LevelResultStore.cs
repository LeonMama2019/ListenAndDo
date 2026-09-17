using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LevelResultRecord
{
    public int level;
    public int questionCount;
    public int firstTryCorrectCount;
    public float correctRate;
    public float averageAnswerTime;
    public int replayCount;
    public string playedAt;
}

[Serializable]
public class LevelResultHistory
{
    public List<LevelResultRecord> sessions = new List<LevelResultRecord>();
}

/// <summary>
/// Level1〜7の結果をPlayerPrefsへ保存する。
/// 最新結果はレベル別キー、全プレイ履歴はJSONで保持する。
/// </summary>
public static class LevelResultStore
{
    private const string KeyPrefix = "ListenAndDo.Level";
    private const string HistoryKey = "ListenAndDo.ResultHistory.v1";

    private static int activeLevel;
    private static int questionCount;
    private static int firstTryCorrectCount;
    private static int correctAnswerCount;
    private static float totalCorrectAnswerTime;
    private static int replayCount;
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        BeginSessionForScene(SceneManager.GetActiveScene().name);
    }

    public static void RecordAnswer(AnswerLogEntry entry)
    {
        if (entry == null)
            return;

        EnsureCurrentSession();

        if (activeLevel < 1 || activeLevel > 7)
            return;

        if (entry.attemptNumber == 1)
        {
            questionCount++;
            if (entry.isCorrect)
                firstTryCorrectCount++;
        }

        if (entry.isCorrect)
        {
            correctAnswerCount++;
            totalCorrectAnswerTime += Mathf.Max(0f, entry.answerTime);
        }
    }

    public static void RecordSpeakerReplay()
    {
        EnsureCurrentSession();

        if (activeLevel >= 1 && activeLevel <= 7)
            replayCount++;
    }

    public static bool TryGetLatest(int level, out LevelResultRecord result)
    {
        result = null;
        if (level < 1 || level > 7)
            return false;

        string prefix = GetLevelKey(level);
        if (!PlayerPrefs.HasKey(prefix + ".PlayedAt"))
            return false;

        result = new LevelResultRecord
        {
            level = level,
            questionCount = PlayerPrefs.GetInt(prefix + ".QuestionCount", 0),
            firstTryCorrectCount = PlayerPrefs.GetInt(prefix + ".FirstTryCorrectCount", 0),
            correctRate = PlayerPrefs.GetFloat(prefix + ".CorrectRate", 0f),
            averageAnswerTime = PlayerPrefs.GetFloat(prefix + ".AverageAnswerTime", 0f),
            replayCount = PlayerPrefs.GetInt(prefix + ".ReplayCount", 0),
            playedAt = PlayerPrefs.GetString(prefix + ".PlayedAt", string.Empty)
        };
        return true;
    }

    public static LevelResultHistory GetHistory()
    {
        string json = PlayerPrefs.GetString(HistoryKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new LevelResultHistory();

        try
        {
            LevelResultHistory history = JsonUtility.FromJson<LevelResultHistory>(json);
            return history != null && history.sessions != null
                ? history
                : new LevelResultHistory();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("全体結果データの読み込みに失敗しました: " + exception.Message);
            return new LevelResultHistory();
        }
    }

    public static void ClearAllResults()
    {
        for (int level = 1; level <= 7; level++)
        {
            string prefix = GetLevelKey(level);
            PlayerPrefs.DeleteKey(prefix + ".QuestionCount");
            PlayerPrefs.DeleteKey(prefix + ".FirstTryCorrectCount");
            PlayerPrefs.DeleteKey(prefix + ".CorrectRate");
            PlayerPrefs.DeleteKey(prefix + ".AverageAnswerTime");
            PlayerPrefs.DeleteKey(prefix + ".ReplayCount");
            PlayerPrefs.DeleteKey(prefix + ".PlayedAt");
        }

        PlayerPrefs.DeleteKey(HistoryKey);
        PlayerPrefs.Save();
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        int previousLevel = ParseLevel(previousScene.name);
        int nextLevel = ParseLevel(nextScene.name);

        if (previousLevel >= 1 && previousLevel <= 7 && previousLevel != nextLevel)
            CompleteSession();

        BeginSessionForScene(nextScene.name);
    }

    private static void EnsureCurrentSession()
    {
        int sceneLevel = ParseLevel(SceneManager.GetActiveScene().name);
        if (sceneLevel != activeLevel)
            BeginSession(sceneLevel);
    }

    private static void BeginSessionForScene(string sceneName)
    {
        BeginSession(ParseLevel(sceneName));
    }

    private static void BeginSession(int level)
    {
        activeLevel = level;
        questionCount = 0;
        firstTryCorrectCount = 0;
        correctAnswerCount = 0;
        totalCorrectAnswerTime = 0f;
        replayCount = 0;
    }

    private static void CompleteSession()
    {
        if (activeLevel < 1 || activeLevel > 7 || questionCount <= 0)
            return;

        LevelResultRecord result = new LevelResultRecord
        {
            level = activeLevel,
            questionCount = questionCount,
            firstTryCorrectCount = firstTryCorrectCount,
            correctRate = questionCount > 0
                ? (float)firstTryCorrectCount / questionCount
                : 0f,
            averageAnswerTime = correctAnswerCount > 0
                ? totalCorrectAnswerTime / correctAnswerCount
                : 0f,
            replayCount = replayCount,
            playedAt = DateTime.Now.ToString("o")
        };

        SaveLatest(result);
        AppendHistory(result);
        PlayerPrefs.Save();

        Debug.Log(
            $"Level{result.level}結果保存: 正解率={result.correctRate:P0}, " +
            $"平均回答時間={result.averageAnswerTime:F2}秒, 聞き返し={result.replayCount}回");
    }

    private static void SaveLatest(LevelResultRecord result)
    {
        string prefix = GetLevelKey(result.level);
        PlayerPrefs.SetInt(prefix + ".QuestionCount", result.questionCount);
        PlayerPrefs.SetInt(prefix + ".FirstTryCorrectCount", result.firstTryCorrectCount);
        PlayerPrefs.SetFloat(prefix + ".CorrectRate", result.correctRate);
        PlayerPrefs.SetFloat(prefix + ".AverageAnswerTime", result.averageAnswerTime);
        PlayerPrefs.SetInt(prefix + ".ReplayCount", result.replayCount);
        PlayerPrefs.SetString(prefix + ".PlayedAt", result.playedAt);
    }

    private static void AppendHistory(LevelResultRecord result)
    {
        LevelResultHistory history = GetHistory();
        history.sessions.Add(result);
        PlayerPrefs.SetString(HistoryKey, JsonUtility.ToJson(history));
    }

    private static string GetLevelKey(int level)
    {
        return KeyPrefix + level;
    }

    private static int ParseLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) ||
            !sceneName.StartsWith("Stage", StringComparison.OrdinalIgnoreCase))
            return 0;

        string number = sceneName.Substring("Stage".Length);
        return int.TryParse(number, out int level) ? level : 0;
    }
}
