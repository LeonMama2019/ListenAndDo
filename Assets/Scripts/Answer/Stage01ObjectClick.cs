using UnityEngine;

public class Stage01ObjectClick : MonoBehaviour
{
    [SerializeField] private AnswerStage01 answerStage01;

    private void OnMouseDown()
    {
       

        if (answerStage01 != null)
        {
            answerStage01.OnObjectClicked();
        }
    }
}