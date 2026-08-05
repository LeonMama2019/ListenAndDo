using UnityEngine;

public class AnswerStage01 : MonoBehaviour
{
    [Header("判定したいオブジェクト")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;

    [Header("参照")]
    [SerializeField] private HandListSelector handListSelector;
    [SerializeField] private TutorialStage01 tutorialStage01;

    [Header("チュートリアル開始までの時間")]
    [SerializeField] private float waitTime = 5f;

    // HandList用タイマー
    private float handTutorialTime = 0f;

    // Speaker用タイマー
    private float speakerTutorialTime = 0f;

    // 同じ状態で何度も呼ばないためのフラグ
    private bool handTutorialShown = false;
    private bool speakerTutorialShown = false;

    private void Update()
    {
        bool handSelected = handListSelector.IsHandSelected();

        bool isMouseOverObject =
            IsMouseOverObject(object1) ||
            IsMouseOverObject(object2);

        CheckHandTutorial(handSelected, isMouseOverObject);
        CheckSpeakerTutorial(isMouseOverObject);




    }

    private void CheckHandTutorial(
        bool handSelected,
        bool isMouseOverObject)
    {
        /*
         * Handを選んでいない状態で、
         * マウスを画像の上に置き続けた場合
         */
        if (!handSelected && isMouseOverObject)
        {
            handTutorialTime += Time.deltaTime;

            if (handTutorialTime >= waitTime &&
                !handTutorialShown)
            {
                handTutorialShown = true;

                Debug.Log("HandListチュートリアル開始");
                tutorialStage01.StartTutorial();
            }
        }
        else
        {
            handTutorialTime = 0f;
            handTutorialShown = false;
        }
    }

    private void CheckSpeakerTutorial(bool isMouseOverObject)
    {
        /*
         * どちらの画像の上にもマウスがない状態が
         * 5秒続いた場合
         */
        if (!isMouseOverObject)
        {
            speakerTutorialTime += Time.deltaTime;

            if (speakerTutorialTime >= waitTime &&
                !speakerTutorialShown)
            {
                speakerTutorialShown = true;

                Debug.Log("Speakerチュートリアル開始");
                tutorialStage01.SpeakerTutorial();
            }
        }
        else
        {
            speakerTutorialTime = 0f;
            speakerTutorialShown = false;
        }
    }

    private bool IsMouseOverObject(GameObject obj)
    {
        if (obj == null || Camera.main == null)
            return false;

        Collider2D col = obj.GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogWarning(
                obj.name + " にCollider2Dがありません"
            );

            return false;
        }

        Vector2 mousePosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        return col.OverlapPoint(mousePosition);
    }

    private bool IsCorrectHand(TaskData task)
    {
        if (task == null || task.verb == null)
            return false;

        string selectedHandAction =
            handListSelector.GetCurrentHandAction();

        string correctVerbName =
            task.verb.name.Replace("Verb_", "");

        Debug.Log(
            $"選択した手：{selectedHandAction} / 正解の動詞：{correctVerbName}"
        );

        return selectedHandAction == correctVerbName;
    }
}