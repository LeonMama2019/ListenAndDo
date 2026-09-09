using UnityEngine;
using UnityEngine.SceneManagement;

public class TopSceneManager : MonoBehaviour
{
    /// <summary>
    /// チュートリアルボタンからStage01へ移動する。
    /// </summary>
    public void OpenTutorial()
    {
        LoadScene("Stage01");
    }

    /// <summary>
    /// 各レベルボタンから、InspectorのOnClickでシーン名を指定して移動する。
    /// 例: Stage02, Stage03
    /// </summary>
    public void OpenLevel(string sceneName)
    {
        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("TopSceneManager: シーン名が設定されていません");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"TopSceneManager: Scene '{sceneName}' がBuild Settings / Build Profilesに登録されていません");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
