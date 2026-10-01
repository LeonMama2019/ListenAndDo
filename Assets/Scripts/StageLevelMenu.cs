using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One Stage scene receives the selected level; completion is saved per stage.
public class StageLevelMenu : MonoBehaviour
{
    public static int SelectedStage { get; private set; } = 1;
    public static int SelectedLevel { get; private set; } = 1;
    [SerializeField] private Sprite unlockedLevel2;
    [SerializeField] private Sprite lockedLevel2;

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

    public static void MarkCompleted(int stage, int level)
    {
        PlayerPrefs.SetInt(CompletionKey(stage, level), 1);
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(int stage, int level)
    {
        if (level == 1) return true;
        if (level != 2) return false; // Later gameplay levels are not connected yet.
        if (PlayerPrefs.GetInt(CompletionKey(stage, 1), 0) == 1) return true;
        StageLevelResult previous = StageLevelResultRecorder.Load(stage, 1);
        return previous != null && previous.questions != null && previous.questions.Count == 7;
    }

    private static string CompletionKey(int stage, int level)
    {
        return $"ListenAndDo.Stage{stage}.Level{level}.Completed";
    }

    private void Start()
    {
        ConnectLevel(1);
        ConnectLevel(2);
    }

    private void ConnectLevel(int level)
    {
        Transform target = Find("Level" + level);
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        bool unlocked = IsUnlocked(SelectedStage, level);
        if (level == 2 && image != null)
            image.sprite = unlocked ? unlockedLevel2 : lockedLevel2;
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
