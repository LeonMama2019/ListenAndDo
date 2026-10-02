using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageSceneButton : MonoBehaviour
{
    [SerializeField] private int stageNumber = 1;
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OpenStage);
    }
    public void OpenStage() => StageLevelMenu.OpenStage(stageNumber);
}
