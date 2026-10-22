using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Uses the existing Result scene objects; no layout or artwork is replaced.
public class ResultHistoryScreen : MonoBehaviour
{
    private const int PageSize = 6;
    private Transform panel, left, right;
    private int stage = 1, level = 1, page;
    private List<StageLevelResult> history;
    private readonly Image[] stageSelection = new Image[6];
    private readonly Image[] levelSelection = new Image[8];
    private Button more;

    private void Start()
    {
        panel = transform.Find("Panel");
        if (panel == null) return;
        left = Child(panel, "Imagel");
        right = Child(panel, "Imager");
        if (left == null || right == null) return;
        for (int i = 1; i <= 6; i++)
        {
            int selected = i;
            Transform target = Child(panel, "Stage" + i + "button");
            Button button = Bind(target, () => { stage = selected; page = 0; Reload(); });
            stageSelection[i - 1] = SelectionOverlay(button);
        }
        for (int i = 1; i <= 8; i++)
        {
            int selected = i;
            Transform target = Child(panel, "levelbutton" + i);
            Button button = Bind(target, () => { level = selected; page = 0; Reload(); });
            levelSelection[i - 1] = SelectionOverlay(button);
        }
        Bind(Child(panel, "Back"), () => LoadScene("Top"));
        more = Bind(Child(right, "more"), NextPage);
        TMP_Text name = Text(panel, "Name");
        if (name != null) { name.richText = false; name.text = PlayerProfileStore.CurrentName; }
        Reload();
    }

    private void Reload()
    {
        history = StageLevelResultRecorder.LoadHistory(stage, level);
        history.RemoveAll(r => r == null || r.questions == null || r.questions.Count == 0);
        history.Sort((a, b) => a.attemptNumber.CompareTo(b.attemptNumber));
        for (int i = 0; i < stageSelection.Length; i++)
            if (stageSelection[i] != null) stageSelection[i].gameObject.SetActive(stage == i + 1);
        for (int i = 0; i < levelSelection.Length; i++)
            if (levelSelection[i] != null) levelSelection[i].gameObject.SetActive(level == i + 1);
        PopulateComparison(Child(left, "firsttime"), history.Count > 0 ? history[0] : null);
        PopulateComparison(Child(left, "lasttime"), history.Count > 0 ? history[history.Count - 1] : null);
        PopulatePage();
    }

    private void PopulateComparison(Transform root, StageLevelResult result)
    {
        if (root == null) return;
        ResultRecordSummary summary = ResultRecordSummary.From(result);
        Set(root, "count", summary.hasRecord ? result.attemptNumber + "回目" : "--回目");
        Transform rate = Child(root, "Accuracy rate");
        SetNumber(rate, summary.hasRecord ? summary.correctPercent.ToString() : "--");
        Transform time = Child(root, "time");
        int seconds = Mathf.RoundToInt(summary.averageSeconds);
        Set(time, "number", summary.hasRecord ? (seconds / 60).ToString() : "--");
        Set(time, "second", summary.hasRecord ? (seconds % 60).ToString("00") : "--");
        SetNumber(Child(root, "miss"), summary.hasRecord ? summary.misses.ToString() : "--");
        SetNumber(Child(root, "repeat"), summary.hasRecord ? summary.replays.ToString() : "--");
    }

    private void PopulatePage()
    {
        // Scene row 1 is the bottom row; row 6 is the top row.
        // Place the oldest record of this page at the bottom and newer ones above.
        int end = history.Count - page * PageSize;
        int start = Math.Max(0, end - PageSize);
        for (int row = 1; row <= PageSize; row++)
        {
            int index = start + row - 1;
            StageLevelResult entry = index < end ? history[index] : null;
            ResultRecordSummary summary = ResultRecordSummary.From(entry);
            SetColumn(1, "count" + row, entry == null ? "--回目" : entry.attemptNumber + "回目");
            SetColumn(2, "date" + row, entry == null ? "--" : DateLabel(entry.completedAtUtc));
            SetColumn(3, "rate" + row, summary.hasRecord ? summary.correctPercent + "％" : "--％");
            SetColumn(4, "time" + row, summary.hasRecord ? TimeLabel(summary.averageSeconds) : "--:--");
            SetColumn(5, "miss" + row, summary.hasRecord ? summary.misses + "回" : "--回");
            SetColumn(6, "repeat" + row, summary.hasRecord ? summary.replays + "回" : "--回");
        }
        bool hasOlder = (page + 1) * PageSize < history.Count;
        if (more != null)
        {
            more.interactable = hasOlder || page > 0;
            TMP_Text label = more.GetComponent<TMP_Text>() ?? more.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = hasOlder ? "もっと前のきろく" : page > 0 ? "さいきんのきろくへ" : "もっと前のきろく";
        }
    }

    private void NextPage()
    {
        page = (page + 1) * PageSize < history.Count ? page + 1 : 0;
        PopulatePage();
    }

    private void SetColumn(int column, string name, string value) => Set(Child(right, "title" + column), name, value);

    private static string DateLabel(string utc)
    {
        if (!DateTime.TryParse(utc, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTime value)) return "--";
        return value.ToLocalTime().ToString("yy-MM/dd", CultureInfo.InvariantCulture);
    }
    private static string TimeLabel(float value)
    {
        int seconds = Mathf.RoundToInt(value);
        return (seconds / 60) + ":" + (seconds % 60).ToString("00");
    }

    private static Image SelectionOverlay(Button button)
    {
        if (button == null) return null;
        Image source = button.targetGraphic as Image;
        Sprite selected = button.spriteState.selectedSprite;
        if (source == null || selected == null) return null;
        // EventSystem focus can only select one object. Separate overlays keep both
        // the chosen stage and chosen level visible while preserving hover sprites.
        var state = button.spriteState;
        state.selectedSprite = state.highlightedSprite;
        button.spriteState = state;
        var go = new GameObject("RecordSelection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(button.transform, false);
        go.transform.SetAsFirstSibling();
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image overlay = go.GetComponent<Image>();
        overlay.sprite = selected;
        overlay.type = source.type;
        overlay.raycastTarget = false;
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private static Button Bind(Transform target, UnityEngine.Events.UnityAction action)
    {
        if (target == null) return null;
        Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        if (button.targetGraphic == null) button.targetGraphic = target.GetComponent<Graphic>();
        if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        return button;
    }
    private static void LoadScene(string name)
    {
        if (Application.CanStreamedLevelBeLoaded(name)) SceneManager.LoadScene(name);
        else Debug.LogError("Scene is missing from Build Profiles: " + name);
    }
    private static string Normalize(string name) => name.Replace(" ", "").Replace("'", "").ToLowerInvariant();
    private static Transform Child(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
            if (Normalize(child.name) == Normalize(name)) return child;
        return null;
    }
    private static TMP_Text Text(Transform parent, string name) => Child(parent, name)?.GetComponent<TMP_Text>();
    private static void Set(Transform parent, string name, string value)
    {
        TMP_Text text = Text(parent, name);
        if (text != null) text.text = value;
    }
    private static void SetNumber(Transform root, string value)
    {
        if (root == null) return;
        Transform target = Child(root, "number") ?? Child(root, "number (1)");
        TMP_Text text = target == null ? null : target.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }
}

public struct ResultRecordSummary
{
    public bool hasRecord;
    public int correctPercent, misses, replays;
    public float averageSeconds;

    public static ResultRecordSummary From(StageLevelResult result)
    {
        ResultRecordSummary summary = new ResultRecordSummary();
        if (result?.questions == null) return summary;
        int count = 0, correct = 0;
        float seconds = 0;
        foreach (var question in result.questions)
        {
            if (question == null) continue;
            count++;
            if (question.wrongAnswerCount == 0) correct++;
            summary.misses += question.wrongAnswerCount;
            summary.replays += question.replayCount;
            seconds += question.timeToCorrectSeconds;
        }
        if (count == 0) return summary;
        summary.hasRecord = true;
        summary.correctPercent = Mathf.RoundToInt(100f * correct / count);
        // Use the number actually completed; some configured stages have fewer than 7.
        summary.averageSeconds = seconds / count;
        return summary;
    }
}
