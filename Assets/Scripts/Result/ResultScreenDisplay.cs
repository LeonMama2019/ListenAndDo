using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resultシーンを開いたとき、Level1〜7の最新結果を各UIへ表示する。
/// UIは各levelルート以下のオブジェクト名から自動取得する。
/// </summary>
public class ResultScreenDisplay : MonoBehaviour
{
    private const int DisplayLevelCount = 7;
    private const int ResponseCount = 7;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Equals("Result", StringComparison.OrdinalIgnoreCase))
            return;

        GameObject host = GameObject.Find("ResultAccordion");
        if (host != null && host.GetComponent<ResultScreenDisplay>() == null)
            host.AddComponent<ResultScreenDisplay>();
    }

    private void Start()
    {
        Populate();
    }

    public void Populate()
    {
        Scene scene = gameObject.scene;

        for (int level = 1; level <= DisplayLevelCount; level++)
        {
            Transform levelRoot = FindInScene(scene, "level" + level);
            if (levelRoot == null)
            {
                Debug.LogWarning("Result画面に level" + level + " が見つかりません。");
                continue;
            }

            if (!LevelResultStore.TryGetLatest(level, out LevelResultRecord result))
            {
                ShowNoResult(levelRoot);
                continue;
            }

            SetText(levelRoot, "Responserate",
                Mathf.RoundToInt(result.correctRate * 100f) + "%");
            SetText(levelRoot, "Responsetime",
                FormatSeconds(result.totalAnswerTime));
            SetText(levelRoot, "NumberRepeat",
                result.replayCount + "回");

            for (int i = 0; i < ResponseCount; i++)
            {
                string value = result.answerTimes != null && i < result.answerTimes.Count
                    ? FormatSeconds(result.answerTimes[i])
                    : "--";
                SetText(levelRoot, "Res" + (i + 1), value);
            }
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

    private static void SetText(Transform root, string objectName, string value)
    {
        TMP_Text target = FindText(root, objectName);
        if (target != null)
            target.text = value;
        else
            Debug.LogWarning(root.name + " 内に " + objectName + " が見つかりません。");
    }

    private static TMP_Text FindText(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (!child.name.Trim().Equals(objectName, StringComparison.OrdinalIgnoreCase))
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                return text;
        }

        return null;
    }

    private static Transform FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Trim().Equals(objectName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }
}
