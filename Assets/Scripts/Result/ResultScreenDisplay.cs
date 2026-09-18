using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resultシーンを開いたとき、保存済みの最新結果をLevel別に表示する。
/// level1、level2…というHierarchy名を自動検出するため、Level追加時も
/// 同じ命名規則ならコード変更なしで表示できる。
/// </summary>
public class ResultScreenDisplay : MonoBehaviour
{
    private const int ResponseCount = 7;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Equals(
                "Result",
                StringComparison.OrdinalIgnoreCase))
            return;

        GameObject host = GameObject.Find("ResultAccordion");
        if (host != null &&
            host.GetComponent<ResultScreenDisplay>() == null)
        {
            host.AddComponent<ResultScreenDisplay>();
        }
    }

    private void Start()
    {
        Populate();
    }

    public void Populate()
    {
        SortedDictionary<int, Transform> levelRoots =
            FindLevelRoots(gameObject.scene);

        foreach (KeyValuePair<int, Transform> pair in levelRoots)
            PopulateLevel(pair.Key, pair.Value);
    }

    private static void PopulateLevel(int level, Transform levelRoot)
    {
        if (!LevelResultStore.TryGetLatest(
                level,
                out LevelResultRecord result))
        {
            ShowNoResult(levelRoot);
            return;
        }

        // Level○.CorrectRate
        SetText(
            levelRoot,
            "Responserate",
            Mathf.RoundToInt(result.correctRate * 100f) + "%");

        // Level○.TotalAnswerTime
        SetText(
            levelRoot,
            "Responsetime",
            FormatSeconds(result.totalAnswerTime));

        // Level○.ReplayCount
        SetText(
            levelRoot,
            "NumberRepeat",
            result.replayCount + "回");

        // Level○.AnswerTime1〜7
        for (int i = 0; i < ResponseCount; i++)
        {
            string value =
                result.answerTimes != null &&
                i < result.answerTimes.Count
                    ? FormatSeconds(result.answerTimes[i])
                    : "--";

            SetText(levelRoot, "Res" + (i + 1), value);
        }
    }

    private static void ShowNoResult(Transform levelRoot)
    {
        SetText(levelRoot, "Responserate", "--");
        SetText(levelRoot, "Responsetime", "--");
        SetText(levelRoot, "NumberRepeat", "--");

        for (int i = 1; i <= ResponseCount; i++)
            SetText(levelRoot, "Res" + i, "--");
    }

    private static string FormatSeconds(float seconds)
    {
        return Mathf.Max(0f, seconds).ToString("F1") + "秒";
    }

    private static void SetText(
        Transform root,
        string objectName,
        string value)
    {
        TMP_Text target = FindText(root, objectName);
        if (target != null)
            target.text = value;
        else
            Debug.LogWarning(
                root.name + " 内に " + objectName +
                " が見つかりません。");
    }

    private static TMP_Text FindText(
        Transform root,
        string objectName)
    {
        Transform[] children =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (!child.name.Trim().Equals(
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return null;
    }

    private static SortedDictionary<int, Transform> FindLevelRoots(
        Scene scene)
    {
        SortedDictionary<int, Transform> result =
            new SortedDictionary<int, Transform>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (TryParseLevelRootName(
                        child.name,
                        out int level))
                {
                    result[level] = child;
                }
            }
        }

        return result;
    }

    private static bool TryParseLevelRootName(
        string objectName,
        out int level)
    {
        level = 0;
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        string trimmed = objectName.Trim();
        const string prefix = "level";

        if (!trimmed.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            return false;

        string number = trimmed.Substring(prefix.Length);
        return int.TryParse(number, out level) && level >= 1;
    }
}
