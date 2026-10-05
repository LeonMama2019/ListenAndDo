using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageLevelMenuButton : MonoBehaviour
{
    [SerializeField] private int stageNumber = 1;
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OpenMenu);
    }
    public void OpenMenu() => StageLevelMenu.OpenStageMenu(stageNumber);
}
