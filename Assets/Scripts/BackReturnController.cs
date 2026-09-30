using UnityEngine;
using UnityEngine.SceneManagement;

public class BackReturnController : MonoBehaviour
{
    [SerializeField] private GameObject backReturnPanel;
    [SerializeField] private string levelMenuScene = "LevelMenu";
    [SerializeField] private int stageNumber = 1;

    public const string SelectedStageKey = "SelectedStage";
    public const string SelectedStageSceneKey = "SelectedStageScene";

    private bool returning;

    private void Awake()
    {
        if (backReturnPanel != null)
            backReturnPanel.SetActive(false);
    }

    public void Open()
    {
        if (backReturnPanel == null || returning)
            return;

        backReturnPanel.transform.SetAsLastSibling();
        backReturnPanel.SetActive(true);
    }

    public void No()
    {
        if (backReturnPanel != null && !returning)
            backReturnPanel.SetActive(false);
    }

    public void Yes()
    {
        if (returning)
            return;

        if (!Application.CanStreamedLevelBeLoaded(levelMenuScene))
        {
            Debug.LogError("BackReturn: Add " + levelMenuScene + " to the Build Profiles scene list.", this);
            return;
        }

        returning = true;
        // Keep stage selection only. Do not save an unfinished quiz result.
        PlayerPrefs.SetInt(SelectedStageKey, stageNumber);
        PlayerPrefs.SetString(SelectedStageSceneKey, gameObject.scene.name);
        PlayerPrefs.Save();
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        SceneManager.LoadScene(levelMenuScene);
    }
}
