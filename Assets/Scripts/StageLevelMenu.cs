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
    [Serializable]
    private class LevelImages
    {
        public Sprite unlocked;
        public Sprite locked;
    }
    // Index 0 = Level2, index 6 = Level8.
    [SerializeField] private LevelImages[] levelImages = new LevelImages[7];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        SelectedStage = 1;
        SelectedLevel = 1;
    }

    public static void OpenStageMenu(int stage)
    {
        if (stage < 1) return;
        SelectedStage = stage;
        SelectedLevel = 1;
        SceneManager.LoadScene("LevelMenu");
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
        return previous != null && previous.questions != null && previous.questions.Count == 7;
    }

    private static string CompletionKey(int stage, int level)
    {
        return $"ListenAndDo.Stage{stage}.Level{level}.Completed";
    }

    private void Start()
    {
        for (int level = 1; level <= 8; level++) ConnectLevel(level);
        UpdateUnlockedCount();
    }

    private void UpdateUnlockedCount()
    {
        int unlockedCount = 0;
        for (int level = 1; level <= 8; level++)
            if (IsUnlocked(SelectedStage, level)) unlockedCount++;

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
        bool unlocked = IsUnlocked(SelectedStage, level);
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

    public void PlayLevel(int level)
    {
        if (!IsUnlocked(SelectedStage, level)) return;
        string scene = "Stage" + SelectedStage;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Add scene to Build Profiles: " + scene);
            return;
        }
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
