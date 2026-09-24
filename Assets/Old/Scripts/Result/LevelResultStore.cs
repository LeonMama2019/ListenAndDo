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
    public float totalAnswerTime;
    public List<float> answerTimes = new List<float>();
    public int replayCount;
    public string playedAt;
}

[Serializable]
public class LevelResultHistory
{
    public List<LevelResultRecord> sessions = new List<LevelResultRecord>();
}

/// <summary>
/// 各Levelの結果をPlayerPrefsへ保存する。
/// Level番号に上限を設けず、追加されたStageも同じキー形式で保存する。
/// </summary>
public static class LevelResultStore
{
    private const string KeyPrefix = "ListenAndDo.Level";
    private const string HistoryKey = "ListenAndDo.ResultHistory.v1";
    private const string HighestSavedLevelKey = "ListenAndDo.HighestSavedLevel";

    private static int activeLevel;
    private static int questionCount;
    private static int firstTryCorrectCount;
    private static int correctAnswerCount;
    private static float totalCorrectAnswerTime;
    private static readonly List<float> answerTimes = new List<float>();
    private static int replayCount;
    private static bool sessionCompleted;
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

        if (activeLevel < 1)
            return;

        if (entry.attemptNumber == 1)
        {
            questionCount++;
            if (entry.isCorrect)
                firstTryCorrectCount++;
        }

        if (entry.isCorrect)
        {
            float answerTime = Mathf.Max(0f, entry.answerTime);
            correctAnswerCount++;
            totalCorrectAnswerTime += answerTime;
            answerTimes.Add(answerTime);
        }
    }

    public static void RecordSpeakerReplay()
    {
        EnsureCurrentSession();

        if (activeLevel >= 1)
            replayCount++;
    }

    /// <summary>
    /// Stage終了直前に呼び、現在の結果をPlayerPrefsへ確実に保存する。
    /// シーン切替通知から再度呼ばれても二重保存しない。
    /// </summary>
    public static void CompleteCurrentSession()
    {
        EnsureCurrentSession();
        CompleteSession();
    }

    public static bool TryGetLatest(int level, out LevelResultRecord result)
    {
        result = null;
        if (level < 1)
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
            totalAnswerTime = PlayerPrefs.GetFloat(
                prefix + ".TotalAnswerTime",
                PlayerPrefs.GetFloat(prefix + ".AverageAnswerTime", 0f) *
                PlayerPrefs.GetInt(prefix + ".QuestionCount", 0)),
            answerTimes = LoadAnswerTimes(prefix),
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
        int highestLevel = Mathf.Max(
            8,
            PlayerPrefs.GetInt(HighestSavedLevelKey, 8));

        for (int level = 1; level <= highestLevel; level++)
        {
            string prefix = GetLevelKey(level);
            PlayerPrefs.DeleteKey(prefix + ".QuestionCount");
            PlayerPrefs.DeleteKey(prefix + ".FirstTryCorrectCount");
            PlayerPrefs.DeleteKey(prefix + ".CorrectRate");
            PlayerPrefs.DeleteKey(prefix + ".AverageAnswerTime");
            PlayerPrefs.DeleteKey(prefix + ".TotalAnswerTime");

            int answerTimeCount = PlayerPrefs.GetInt(
                prefix + ".AnswerTimeCount",
                7);
            for (int i = 1; i <= answerTimeCount; i++)
                PlayerPrefs.DeleteKey(prefix + ".AnswerTime" + i);
            PlayerPrefs.DeleteKey(prefix + ".AnswerTimeCount");
            PlayerPrefs.DeleteKey(prefix + ".ReplayCount");
            PlayerPrefs.DeleteKey(prefix + ".PlayedAt");
        }

        PlayerPrefs.DeleteKey(HistoryKey);
        PlayerPrefs.DeleteKey(HighestSavedLevelKey);
        PlayerPrefs.Save();
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        int previousLevel = ParseLevel(previousScene.name);
        int nextLevel = ParseLevel(nextScene.name);

        if (previousLevel >= 1 && previousLevel != nextLevel)
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
        answerTimes.Clear();
        replayCount = 0;
        sessionCompleted = false;
    }

    private static void CompleteSession()
    {
        if (sessionCompleted || activeLevel < 1 || questionCount <= 0)
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
            totalAnswerTime = totalCorrectAnswerTime,
            answerTimes = new List<float>(answerTimes),
            replayCount = replayCount,
            playedAt = DateTime.Now.ToString("o")
        };

        SaveLatest(result);
        AppendHistory(result);
        PlayerPrefs.Save();
        sessionCompleted = true;

        Debug.Log(
            $"Level{result.level}結果保存: 正解率={result.correctRate:P0}, " +
            $"合計回答時間={result.totalAnswerTime:F2}秒, 聞き返し={result.replayCount}回");
    }

    private static void SaveLatest(LevelResultRecord result)
    {
        string prefix = GetLevelKey(result.level);
        PlayerPrefs.SetInt(prefix + ".QuestionCount", result.questionCount);
        PlayerPrefs.SetInt(prefix + ".FirstTryCorrectCount", result.firstTryCorrectCount);
        PlayerPrefs.SetFloat(prefix + ".CorrectRate", result.correctRate);
        PlayerPrefs.SetFloat(prefix + ".AverageAnswerTime", result.averageAnswerTime);
        PlayerPrefs.SetFloat(prefix + ".TotalAnswerTime", result.totalAnswerTime);

        int previousTimeCount = PlayerPrefs.GetInt(
            prefix + ".AnswerTimeCount",
            0);
        int answerTimeCount = result.answerTimes != null
            ? result.answerTimes.Count
            : 0;

        PlayerPrefs.SetInt(prefix + ".AnswerTimeCount", answerTimeCount);
        for (int i = 0; i < answerTimeCount; i++)
            PlayerPrefs.SetFloat(
                prefix + ".AnswerTime" + (i + 1),
                result.answerTimes[i]);

        for (int i = answerTimeCount + 1; i <= previousTimeCount; i++)
            PlayerPrefs.DeleteKey(prefix + ".AnswerTime" + i);

        int highestSavedLevel = PlayerPrefs.GetInt(
            HighestSavedLevelKey,
            0);
        if (result.level > highestSavedLevel)
            PlayerPrefs.SetInt(HighestSavedLevelKey, result.level);

        PlayerPrefs.SetInt(prefix + ".ReplayCount", result.replayCount);
        PlayerPrefs.SetString(prefix + ".PlayedAt", result.playedAt);
    }

    private static void AppendHistory(LevelResultRecord result)
    {
        LevelResultHistory history = GetHistory();
        history.sessions.Add(result);
        PlayerPrefs.SetString(HistoryKey, JsonUtility.ToJson(history));
    }

    private static List<float> LoadAnswerTimes(string prefix)
    {
        List<float> times = new List<float>();
        int count = PlayerPrefs.GetInt(
            prefix + ".AnswerTimeCount",
            0);

        // 旧データにはCountがないため、連番キーを自動検出する。
        if (count <= 0)
        {
            while (PlayerPrefs.HasKey(
                prefix + ".AnswerTime" + (count + 1)))
            {
                count++;
            }
        }

        for (int i = 1; i <= count; i++)
        {
            float time = PlayerPrefs.GetFloat(
                prefix + ".AnswerTime" + i,
                -1f);
            if (time >= 0f)
                times.Add(time);
        }

        return times;
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
