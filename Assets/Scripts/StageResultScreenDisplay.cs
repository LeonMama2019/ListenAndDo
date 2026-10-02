using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Result selects saved records; LevelResult shares the detail view with game completion.</summary>
public class StageResultScreenDisplay : MonoBehaviour
{
    public static int SelectedStage { get; private set; } = 1;
    public static int SelectedLevel { get; private set; } = 1;
    public static bool FromRecords { get; private set; }

    private Transform detail;
    private Transform stageMenu;

    public static void OpenAfterGame(int stage, int level)
    {
        OpenLevel(stage, level, false);
    }

    public static void OpenFromRecords(int stage, int level)
    {
        OpenLevel(stage, level, true);
    }

    private static void OpenLevel(int stage, int level, bool fromRecords)
    {
        if (stage < 1 || level < 1 || level > 8)
        {
            Debug.LogError("Invalid result stage or level.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded("LevelResult"))
        {
            Debug.LogError("Add LevelResult to the Build Profiles scene list.");
            return;
        }
        SelectedStage = stage;
        SelectedLevel = level;
        FromRecords = fromRecords;
        SceneManager.LoadScene("LevelResult");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SelectedStage = 1;
        SelectedLevel = 1;
        FromRecords = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LevelResult" && scene.name != "Result") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<StageResultScreenDisplay>(true) != null) return;
        GameObject host = new GameObject("ResultDisplay");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<StageResultScreenDisplay>();
    }

    private void Start()
    {
        Transform canvas = FindInScene("Canvas");
        if (canvas == null) return;
        detail = canvas.Find("DetailPanel");
        stageMenu = canvas.Find("Panel");
        if (detail == null) return;

        if (gameObject.scene.name == "LevelResult")
        {
            if (stageMenu != null) stageMenu.gameObject.SetActive(false);
            detail.gameObject.SetActive(true);
            Populate();
            Bind(Find(detail, "toStagemenu"), ReturnToMenu);
            Bind(Find(detail, "nextbutton"), () => StageLevelMenu.OpenStageMenu(SelectedStage));
        }
        else
        {
            FromRecords = true;
            detail.gameObject.SetActive(false);
            if (stageMenu != null)
            {
                stageMenu.gameObject.SetActive(true);
                foreach (Transform child in stageMenu.GetComponentsInChildren<Transform>(true))
                {
                    if (!TryNumber(child.name, "Stage", out int stage)) continue;
                    Transform target = Find(child, "Detail");
                    int number = stage;
                    Bind(target != null ? target : child, () => ShowLevels(number));
                }
                Bind(Find(stageMenu, "Back"), () => LoadScene("Top"));
            }
            Bind(Find(detail, "toStagemenu"), ShowStageMenu);
        }
    }

    public void Populate()
    {
        if (detail == null) return;
        SetText(detail, "Level", "レベル" + SelectedLevel);
        Transform plate = Find(detail, "Plate");
        if (plate == null) return;
        plate.gameObject.SetActive(true);

        StageLevelResult result = StageLevelResultRecorder.Load(SelectedStage, SelectedLevel);
        bool hasResult = result != null && result.questions != null && result.questions.Count > 0;
        int correct = 0, misses = 0, replays = 0;
        float total = 0f;
        if (hasResult)
        {
            foreach (StageQuestionResult question in result.questions)
            {
                if (question == null) continue;
                if (question.wrongAnswerCount == 0) correct++;
                misses += question.wrongAnswerCount;
                replays += question.replayCount;
                total += question.timeToCorrectSeconds;
            }
        }
        // Units are already separate fixText objects in the scene.
        SetText(plate, "Percent", hasResult
            ? Mathf.RoundToInt(100f * correct / result.questions.Count).ToString() : "--");
        SetText(plate, "Time", hasResult ? Seconds(total) : "--");
        SetText(plate, "Miss", hasResult ? misses.ToString() : "--");
        SetText(plate, "Rehear", hasResult ? replays.ToString() : "--");

        Transform parent = Find(detail, "levelParent");
        if (parent == null) return;
        for (int i = 1; i <= 7; i++)
        {
            Transform tile = Find(parent, "level" + i);
            if (tile == null) continue;
            StageQuestionResult question = hasResult
                ? result.questions.Find(q => q != null && q.questionNumber == i) : null;
            SetText(tile, "Time", question == null ? "--" : Seconds(question.timeToCorrectSeconds));
        }
    }

    private void ShowLevels(int stage)
    {
        SelectedStage = stage;
        if (stageMenu != null) stageMenu.gameObject.SetActive(false);
        detail.gameObject.SetActive(true);
        SetText(detail, "Level", "ステージ" + stage);
        Transform plate = Find(detail, "Plate");
        if (plate != null) plate.gameObject.SetActive(false);
        Transform parent = Find(detail, "levelParent");
        if (parent == null) return;
        foreach (Transform tile in parent)
        {
            if (!TryNumber(tile.name, "level", out int level)) continue;
            int number = level;
            StageLevelResult result = StageLevelResultRecorder.Load(stage, number);
            SetText(tile, "Time", result == null ? "--" : Seconds(result.totalAnswerSeconds));
            Bind(tile, () => OpenFromRecords(stage, number));
        }
    }

    private void ShowStageMenu()
    {
        detail.gameObject.SetActive(false);
        if (stageMenu != null) stageMenu.gameObject.SetActive(true);
    }

    public void ReturnToMenu()
    {
        if (FromRecords) LoadScene("Result");
        else StageLevelMenu.OpenStageMenu(SelectedStage);
    }

    private static void LoadScene(string name)
    {
        if (Application.CanStreamedLevelBeLoaded(name)) SceneManager.LoadScene(name);
        else Debug.LogError("Scene is missing from Build Profiles: " + name);
    }

    private Transform FindInScene(string name)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            Transform found = Find(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private static void SetText(Transform root, string name, string value)
    {
        Transform found = Find(root, name);
        TMP_Text text = found == null ? null : found.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
        else Debug.LogWarning(root.name + ": missing TMP text " + name);
    }

    private static string Seconds(float value)
    {
        return Mathf.Max(0f, value).ToString("F1", CultureInfo.InvariantCulture);
    }

    private static bool TryNumber(string name, string prefix, out int number)
    {
        number = 0;
        string trimmed = name.Trim();
        return trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(trimmed.Substring(prefix.Length), out number) && number > 0;
    }

    private static void Bind(Transform target, UnityEngine.Events.UnityAction action)
    {
        if (target == null) return;
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.gameObject.AddComponent<Button>();
        if (button.targetGraphic == null) button.targetGraphic = target.GetComponent<Graphic>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
