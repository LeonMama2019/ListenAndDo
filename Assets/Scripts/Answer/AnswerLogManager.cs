using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class AnswerLogEntry
{
    public string questionId;
    public int attemptNumber;
    public string correctObject;
    public string selectedObject;
    public string correctHand;
    public string selectedHand;
    public bool isCorrect;
    public float objectSelectionTime;
    public float answerTime;
    public string answeredAt;
}

[Serializable]
public class AnswerLogData
{
    public List<AnswerLogEntry> answers = new List<AnswerLogEntry>();
}

public static class AnswerLogManager
{
    private const string FileName = "answer_log.json";
    private const string WebGlPlayerPrefsKey = "ListenAndDo.AnswerLogJson";

    private static AnswerLogData logData;
    private static string LogFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    public static string SavePath
    {
        get { return LogFilePath; }
    }

    public static void AddAnswer(AnswerLogEntry entry)
    {
        EnsureLoaded();
        logData.answers.Add(entry);
        Save();
    }

    private static void EnsureLoaded()
    {
        if (logData != null)
            return;

        string json = string.Empty;

        try
        {
            if (File.Exists(LogFilePath))
            {
                json = File.ReadAllText(LogFilePath);
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            else
            {
                json = PlayerPrefs.GetString(WebGlPlayerPrefsKey, string.Empty);
            }
#endif
        }
        catch (Exception exception)
        {
            Debug.LogWarning("回答ログの読み込みに失敗しました: " + exception.Message);
        }

        if (!string.IsNullOrEmpty(json))
        {
            logData = JsonUtility.FromJson<AnswerLogData>(json);
        }

        if (logData == null || logData.answers == null)
        {
            logData = new AnswerLogData();
        }
    }

    private static void Save()
    {
        string json = JsonUtility.ToJson(logData, true);

        try
        {
            File.WriteAllText(LogFilePath, json);

#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString(WebGlPlayerPrefsKey, json);
            PlayerPrefs.Save();
#endif

            Debug.Log("回答ログを保存しました: " + LogFilePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("回答ログの保存に失敗しました: " + exception.Message);
        }
    }
}
