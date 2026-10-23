using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class StageTopBackButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ReturnToTop);
    }

    private void ReturnToTop()
    {
        if (!Application.CanStreamedLevelBeLoaded("Top"))
        {
            Debug.LogError("StageTopBackButton: Add Top to Build Profiles.", this);
            return;
        }

        SceneManager.LoadScene("Top");
    }
}
