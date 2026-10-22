using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One Stage scene receives the selected level; completion is saved per stage.
public class StageLevelMenu : MonoBehaviour
{
    public static int SelectedStage { get; private set; } = 1;
    public static int SelectedLevel { get; private set; } = 1;
    [SerializeField] private Transform tutorialButton;
    private static int requestedTutorialStage;
    public static bool IsTutorialReplay(int stage) => requestedTutorialStage == stage;
    public static bool ReturnFromTutorial(int stage)
    {
        if (!IsTutorialReplay(stage)) return false;
        requestedTutorialStage = 0;
        OpenStageMenu(stage);
        return true;
    }

    [Serializable]
    private class LevelImages
    {
        public Sprite unlocked;
        public Sprite locked;
    }
    [Tooltip("Stage5で設定済みのレベルだけを有効にするためのデータ")]
    [SerializeField] private Stage5TaskData stage5Data;
    private bool IsConfigured(int level) => SelectedStage != 5 || (stage5Data != null && stage5Data.HasLevel(level));

    // Index 0 = Level2, index 6 = Level8.
    [SerializeField] private LevelImages[] levelImages = new LevelImages[7];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        SelectedStage = 1;
        SelectedLevel = 1;
        requestedTutorialStage = 0;
    }

    public static void OpenStageMenu(int stage)
    {
        if (stage < 1) return;
        SelectStageForMenu(stage);
        SceneManager.LoadScene("LevelMenu");
    }

    public static void SelectStageForMenu(int stage)
    {
        if (stage < 1) return;
        requestedTutorialStage = 0;
        SelectedStage = stage;
        SelectedLevel = 1;
    }

    public static void OpenStage(int stage)
    {
        if (stage < 1) return;
        string scene = "Stage" + stage;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Add scene to Build Profiles: " + scene);
            return;
        }
        requestedTutorialStage = 0;
        SelectedStage = stage;
        SelectedLevel = 1;
        SceneManager.LoadScene(scene);
    }

    public static void MarkCompleted(int stage, int level)
    {
        PlayerPrefs.SetInt(CompletionKey(stage, level), 1);
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(int stage, int level)
    {
        if (stage < 1 || level < 1 || level > 8) return false;
        if (level == 1) return true;
        if (PlayerPrefs.GetInt(CompletionKey(stage, level - 1), 0) == 1) return true;
        StageLevelResult previous = StageLevelResultRecorder.Load(stage, level - 1);
        return previous != null && previous.questions != null
            && (previous.questions.Count == 7 || (stage == 6 && previous.questions.Count == 4));
    }

    private static string CompletionKey(int stage, int level)
    {
        return StageLevelResultRecorder.CompletionKey(stage, level);
    }

    private void Start()
    {
        var back = Find("Back");
        if (back != null)
        {
            var image = back.GetComponent<Image>();
            var button = back.GetComponent<Button>() ?? back.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ReturnToStageTop);
        }
        for (int level = 1; level <= 8; level++) ConnectLevel(level);
        UpdateUnlockedCount();
        ConnectTutorial();
    }

    public void ReturnToStageTop()
    {
        if (!Application.CanStreamedLevelBeLoaded("StageTop"))
        {
            Debug.LogError("Add StageTop to Build Profiles.", this);
            return;
        }
        SceneManager.LoadScene("StageTop");
    }

    private void UpdateUnlockedCount()
    {
        int unlockedCount = 0;
        for (int level = 1; level <= 8; level++)
            if (IsConfigured(level) && IsUnlocked(SelectedStage, level)) unlockedCount++;

        Transform count = Find("count");
        if (count == null) return;
        TMP_Text label = count.GetComponent<TMP_Text>();
        if (label != null) label.text = unlockedCount.ToString();
    }

    private void ConnectLevel(int level)
    {
        Transform target = Find("Level" + level);
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        bool unlocked = IsConfigured(level) && IsUnlocked(SelectedStage, level);
        if (level >= 2 && image != null)
        {
            LevelImages images = levelImages != null && level - 2 < levelImages.Length
                ? levelImages[level - 2] : null;
            Sprite sprite = images == null ? null : (unlocked ? images.unlocked : images.locked);
            if (sprite != null) image.sprite = sprite;
            else Debug.LogWarning("Level" + level + " image is not configured.", this);
        }
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = unlocked;
        // No tint: keep the locked artwork's original colors.
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => PlayLevel(level));
    }

    private void ConnectTutorial()
    {
        var target = tutorialButton != null ? tutorialButton : Find("HowToPlay");
        if (target == null) return;
        bool supported = SelectedStage == 1 || SelectedStage == 2 || SelectedStage == 3 || SelectedStage == 4 || SelectedStage == 5 || SelectedStage == 6;
        target.gameObject.SetActive(supported);
        if (!supported) return;
        var image = target.GetComponent<Image>();
        if (image != null) image.raycastTarget = true;
        var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(PlayTutorial);
    }

    public void PlayTutorial()
    {
        if (SelectedStage != 1 && SelectedStage != 2 && SelectedStage != 3 && SelectedStage != 4 && SelectedStage != 5 && SelectedStage != 6) return;
        string scene = "Stage" + SelectedStage;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Add scene to Build Profiles: " + scene, this);
            return;
        }
        SelectedLevel = 1;
        requestedTutorialStage = SelectedStage;
        SceneManager.LoadScene(scene);
    }

    public void PlayLevel(int level)
    {
        if (!IsConfigured(level) || !IsUnlocked(SelectedStage, level)) return;
        string scene = "Stage" + SelectedStage;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Add scene to Build Profiles: " + scene);
            return;
        }
        requestedTutorialStage = 0;
        SelectedLevel = level;
        SceneManager.LoadScene(scene);
    }

    private Transform Find(string name)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }
}


